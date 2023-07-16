using System.Net;
using System.Net.Sockets;

using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;
using Darkages.Interfaces;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;

using Microsoft.AppCenter.Analytics;

using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using RestSharp;

namespace Darkages.Network.Server;

public sealed class LobbyServer : ServerBase<ILobbyClient>, ILobbyServer<ILobbyClient>
{
    private readonly IClientFactory<LobbyClient> _clientProvider;
    private readonly MServerTable _serverTable;
    private readonly RestClient _restClient = new("https://api.abuseipdb.com/api/v2/check");

    public LobbyServer(
        IClientFactory<LobbyClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<ILobbyClient> lobbyRegistry,
        ILogger<LobbyServer> logger,
        IOptions<Chaos.Networking.Options.ServerOptions> options
    ) : base(redirectManager, packetSerializer, lobbyRegistry, options, logger)
    {
        _clientProvider = clientProvider;
        _serverTable = MServerTable.FromFile("MServerTable.xml");
        IndexHandlers();

    }

    #region OnHandlers


    public ValueTask OnConnectionInfoRequest(ILobbyClient client, in ClientPacket _)
    {
        ValueTask InnerOnConnectionInfoRequest(ILobbyClient localClient)
        {
            localClient.SendConnectionInfo(_serverTable.Hash);

            return default;
        }

        return ExecuteHandler(client, InnerOnConnectionInfoRequest);
    }

    public ValueTask OnVersion(ILobbyClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<VersionArgs>(in packet);

        ValueTask InnerOnVersion(ILobbyClient localClient, VersionArgs localArgs)
        {
            if (localArgs.Version != ServerSetup.Instance.Config.ClientVersion) return default;

            localClient.SendConnectionInfo(_serverTable.Hash);

            return default;
        }

        return ExecuteHandler(client, args, InnerOnVersion);
    }

    public ValueTask OnServerTableRequest(ILobbyClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<ServerTableRequestArgs>(in packet);

        ValueTask InnerOnServerTableRequest(ILobbyClient localClient, ServerTableRequestArgs localArgs)
        {
            var (serverTableRequestType, serverId) = localArgs;
            switch (serverTableRequestType)
            {
                case ServerTableRequestType.ServerId:
                    var connectInfo = new IPEndPoint(_serverTable.Servers[0].Address, _serverTable.Servers[0].Port);
                    var redirect = new Chaos.Networking.Entities.Redirect(EphemeralRandomIdGenerator<uint>.Shared.NextId, new ConnectionInfo { Address = connectInfo.Address, Port = connectInfo.Port },
                        ServerType.Lobby, localClient.Crypto.Key, localClient.Crypto.Seed, $"socket[{localClient.Id}]");
                    RedirectManager.Add(redirect);
                    localClient.SendRedirect(redirect);
                    break;
                case ServerTableRequestType.RequestTable:
                    localClient.SendServerTable(_serverTable.Data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }

        return ExecuteHandler(client, args, InnerOnServerTableRequest);
    }

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILobbyClient client, in ClientPacket packet)
    {
        var handler = ClientHandlers[(byte)packet.OpCode];
        return handler?.Invoke(client, in packet) ?? default;
    }

    protected override void IndexHandlers()
    {
        base.IndexHandlers();

        ClientHandlers[(byte)ClientOpCode.Version] = OnVersion;
        ClientHandlers[(byte)ClientOpCode.ServerTableRequest] = OnServerTableRequest;
    }

    protected override void OnConnection(IAsyncResult ar)
    {
        var serverSocket = (Socket)ar.AsyncState!;
        var clientSocket = serverSocket.EndAccept(ar);
        serverSocket.BeginAccept(OnConnection, serverSocket);
        var client = _clientProvider.CreateClient(clientSocket);

        // ToDo Client BadActor logic & Version Check
        
        var badActor = ClientOnBlackList(client);

        if (badActor)
        {
            ServerSetup.Logger($"{client.RemoteIp} was detected as potentially malicious", LogLevel.Critical);
            client.Disconnect();
            return;
        }

        if (client is not { Connected: true })
        {
            return;
        }

        client.BeginReceive();
        // 0x7E - Handshake
        client.SendAcceptConnection();
    }

    private void OnDisconnect(object? sender, EventArgs e)
    {
        var client = (ILobbyClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    /// <summary>
    /// Client IP Check - Blacklist and BOGON list checks
    /// </summary>
    /// <returns>Boolean, whether or not the IP has been listed as valid</returns>
    private bool ClientOnBlackList(ISocketClient client)
    {
        if (client == null) return true;
        const char delimiter = ':';
        var ipToString = client.Socket.RemoteEndPoint?.ToString();
        var ipSplit = ipToString?.Split(delimiter);
        var ip = ipSplit?[0];
        var tokenSource = new CancellationTokenSource(5000);

        switch (ip)
        {
            case null:
                return true;
            case "208.115.199.29": // uptimerrobot ipaddress - Do not allow it to go further than just pinging our IP
                try
                {
                    client.Socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    client.Socket.Close();
                }
                return false;
            case "127.0.0.1":
            case "192.168.50.1": // Local Development Address withing your network
                ServerSetup.Logger("-----------------------------------");
                ServerSetup.Logger("Loopback IP & (Local) Authorized.");
                return false;
        }

        var bogonCheck = BogonCheck(client, ip);
        if (bogonCheck)
        {
            return true;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
                ServerSetup.Logger("Because of this, you're not protected from attackers");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest();
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", ip);
            request.AddParameter("maxAgeInDays", "180");

            var response = _restClient.ExecuteGetAsync<Ipdb>(request, tokenSource.Token);
            
            if (response.Result.IsSuccessful)
            {
                var json = response.Result.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.Logger("-----------------------------------");
                    ServerSetup.Logger("API Issue with IP database.");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var ipdbResponse = ipdb?.Data?.AbuseConfidenceScore;

                switch (ipdbResponse)
                {
                    case >= 25:
                        Analytics.TrackEvent(
                            $"{ip} had a score of {ipdbResponse} and was blocked from accessing the server.");
                        ServerSetup.Logger("-----------------------------------");
                        ServerSetup.Logger($"{ip} was blocked with a score of {ipdbResponse}.");
                        return true;
                    case >= 0:
                        ServerSetup.Logger("-----------------------------------");
                        ServerSetup.Logger($"{ip} had a score of {ipdbResponse}.");
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.Logger("-----------------------------------");
                        ServerSetup.Logger("API Issue with IP database.");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.Logger("-----------------------------------");
                ServerSetup.Logger("API Issue with IP database.");
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            ServerSetup.Logger("API Timed-out, continuing connection.");
            if (tokenSource.Token.IsCancellationRequested) return false;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger($"{ex}\nUnknown exception in ClientOnBlacklist method.");
            Crashes.TrackError(ex);
            return true;
        }

        return true;
    }

    private readonly HashSet<string> _bogonIPs = new()
    {
        "0.0.0.0", "0.0.0.1", "0.0.0.2", "0.0.0.3", "0.0.0.4", "0.0.0.5", "0.0.0.6", "0.0.0.7", "0.0.0.8",
        "10.0.0.0", "10.0.0.1", "10.0.0.2", "10.0.0.3", "10.0.0.4", "10.0.0.5", "10.0.0.6", "10.0.0.7", "10.0.0.8",
        "100.64.0.0", "100.64.0.1", "100.64.0.2", "100.64.0.3", "100.64.0.4", "100.64.0.5", "100.64.0.6", "100.64.0.7",
        "100.64.0.8", "100.64.0.9", "100.64.0.10",
        "127.0.0.0", "127.0.0.2", "127.0.0.3", "127.0.0.4", "127.0.0.5", "127.0.0.6", "127.0.0.7", "127.0.0.8",
        "169.254.0.0", "169.254.0.1", "169.254.0.2", "169.254.0.3", "169.254.0.4", "169.254.0.5", "169.254.0.6", "169.254.0.7", "169.254.0.8",
        "169.254.0.9","169.254.0.10","169.254.0.11","169.254.0.12","169.254.0.13","169.254.0.14","169.254.0.15","169.254.0.16",
        "172.16.0.0", "172.16.0.1", "172.16.0.2", "172.16.0.3", "172.16.0.4", "172.16.0.5", "172.16.0.6",
        "172.16.0.7","172.16.0.8","172.16.0.9","172.16.0.10","172.16.0.11","172.16.0.12",
        "192.0.0.0", "192.0.0.1", "192.0.0.2", "192.0.0.3", "192.0.0.4", "192.0.0.5", "192.0.0.6", "192.0.0.7", "192.0.0.8",
        "192.0.0.9","192.0.0.10","192.0.0.11","192.0.0.12","192.0.0.13","192.0.0.14","192.0.0.15","192.0.0.16","192.0.0.17",
        "192.0.0.18", "192.0.0.19","192.0.0.20","192.0.0.21","192.0.0.22","192.0.0.23", "192.0.0.24",
        "192.0.2.0","192.0.2.1","192.0.2.2","192.0.2.3","192.0.2.4","192.0.2.5","192.0.2.6","192.0.2.7","192.0.2.8",
        "192.0.2.9","192.0.2.10","192.0.2.11","192.0.2.12","192.0.2.13","192.0.2.14","192.0.2.15","192.0.2.16","192.0.2.17",
        "192.0.2.18","192.0.2.19","192.0.2.20","192.0.2.21","192.0.2.22","192.0.2.23","192.0.2.24",
        "192.168.0.0","192.168.0.1","192.168.0.2","192.168.0.3","192.168.0.4","192.168.0.5","192.168.0.6","192.168.0.7","192.168.0.8",
        "192.168.0.9","192.168.0.10","192.168.0.11","192.168.0.12","192.168.0.13","192.168.0.14","192.168.0.15","192.168.0.16",
        "198.18.0.0","198.18.0.1","198.18.0.2","198.18.0.3","198.18.0.4","198.18.0.5","198.18.0.6","198.18.0.7","198.18.0.8",
        "198.18.0.9","198.18.0.10","198.18.0.11","198.18.0.12","198.18.0.13","198.18.0.14","198.18.0.15",
        "198.51.100.0", "198.51.100.1","198.51.100.2","198.51.100.3","198.51.100.4","198.51.100.5","198.51.100.6","198.51.100.7","198.51.100.8",
        "198.51.100.9","198.51.100.10","198.51.100.11","198.51.100.12","198.51.100.13","198.51.100.14","198.51.100.15","198.51.100.16","198.51.100.17",
        "198.51.100.18","198.51.100.19","198.51.100.20","198.51.100.21","198.51.100.22","198.51.100.23","198.51.100.24",
        "203.0.11.0","203.0.11.1","203.0.11.2","203.0.11.3","203.0.11.4","203.0.11.5","203.0.11.6","203.0.11.7","203.0.11.8","203.0.11.9",
        "203.0.11.10","203.0.11.11","203.0.11.12","203.0.11.13","203.0.11.14","203.0.11.15","203.0.11.16","203.0.11.17","203.0.11.18","203.0.11.19",
        "203.0.11.20","203.0.11.21","203.0.11.22","203.0.11.23","203.0.11.24",
        "224.0.0.0", "224.0.0.1", "224.0.0.2", "224.0.0.3"
    };

    private bool BogonCheck(ISocketClient client, string ip)
    {
        if (client.Socket.RemoteEndPoint == null || ip == null) return true;

        // Add any banned player IPs to the BogonIPs HashSet
        // BogonIPs.Add("banned.player.ip");

        return _bogonIPs.Contains(ip);
    }

    #endregion
}