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

using Newtonsoft.Json;

using RestSharp;

using ServerOptions = Chaos.Networking.Options.ServerOptions;

namespace Darkages.Network.Server;

/// <summary>
/// Connections to the server enter here, in order
///     -> OnConnection (Establishes the connection and checks IP)
///     -> OnVersion (Checks version of client)
///     -> OnServerTableRequest (Sends server table)
/// </summary>
public sealed class LobbyServer : ServerBase<ILobbyClient>, ILobbyServer<ILobbyClient>
{
    private readonly IClientFactory<LobbyClient> _clientProvider;
    private readonly MServerTable _serverTable;
    private readonly RestClient _restClient = new("https://api.abuseipdb.com/api/v2/check");
    private const string InternalIP = "192.168.50.1"; // Cannot use ServerConfig due to value needing to be constant

    public LobbyServer(
        IClientFactory<LobbyClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<ILobbyClient> lobbyRegistry,
        ILogger<LobbyServer> logger) : base(redirectManager, packetSerializer, lobbyRegistry, Microsoft.Extensions.Options.Options.Create(new ServerOptions
        {
            Address = ServerSetup.Instance.IpAddress,
            Port = ServerSetup.Instance.Config.LOBBY_PORT
        }), logger)
    {
        ServerSetup.Instance.LobbyServer = this;
        _clientProvider = clientProvider;
        _serverTable = MServerTable.FromFile("MServerTable.xml");
        IndexHandlers();
    }

    #region OnHandlers

    public ValueTask OnVersion(ILobbyClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<VersionArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnVersion);

        ValueTask InnerOnVersion(ILobbyClient localClient, VersionArgs localArgs)
        {
            if (localArgs.Version != ServerSetup.Instance.Config.ClientVersion)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "You're not using an authorized client. Please visit https://www.TheBuckNetwork.com/Zolian for the latest client.");
                localClient.Disconnect();
                return default;
            }

            localClient.SendConnectionInfo(_serverTable.Hash);
            return default;
        }
    }

    public ValueTask OnServerTableRequest(ILobbyClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<ServerTableRequestArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnServerTableRequest);

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
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "You're not authorized.");
                    localClient.Disconnect();
                    break;
            }

            return default;
        }
    }

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILobbyClient client, in ClientPacket packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[(byte)packet.OpCode];
        if (handler != null) return handler(client, in packet);
        ServerSetup.Logger($"Unknown message to lobby server with code {opCode} from {client.RemoteIp}");
        Analytics.TrackEvent($"Unknown message to lobby server with code {opCode} from {client.RemoteIp}");
        return default;
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

        var ip = clientSocket.RemoteEndPoint as IPEndPoint;
        ServerSetup.Logger($"Lobby connection from {ip}");

        var client = _clientProvider.CreateClient(clientSocket);
        var badActor = ClientOnBlackList(client);

        if (badActor)
        {
            client.Disconnect();
            return;
        }

        if (!ClientRegistry.TryAdd(client))
        {
            ServerSetup.Logger("Two clients ended up with the same id - newest client disconnected");
            client.Disconnect();
            return;
        }

        client.OnDisconnected += OnDisconnect;
        client.BeginReceive();
        // 0x7E - Handshake
        client.SendAcceptConnection();
    }

    private void OnDisconnect(object sender, EventArgs e)
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

        switch (client.RemoteIp.ToString())
        {
            case "208.115.199.29": // uptimerrobot address - Do not allow it to go further than just pinging our IP
                return true;
            case "127.0.0.1":
            case InternalIP:
                return false;
        }

        var bogonCheck = BannedIpCheck(client, client.RemoteIp.ToString());
        if (bogonCheck)
        {
            ServerSetup.Logger("-----------------------------------");
            ServerSetup.Logger($"{client.RemoteIp} is banned and unable to connect");
            return true;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", client.RemoteIp.ToString());
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = _restClient.Execute<Ipdb>(request);

            if (response.IsSuccessful)
            {
                var json = response.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.Logger($"{client.RemoteIp} - API Issue, response is null or length is 0");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                Analytics.TrackEvent($"{client.RemoteIp} has a confidence score of {abuseConfidenceScore}, is using tor: {tor}, and IP type: {usageType}");

                if (tor == true)
                {
                    ServerSetup.Logger("---------Lobby-Server---------");
                    ServerSetup.Logger($"{client.RemoteIp} is using tor and automatically blocked", LogLevel.Warning);
                    return true;
                }

                if (usageType == "Reserved")
                {
                    ServerSetup.Logger("---------Lobby-Server---------");
                    ServerSetup.Logger($"{client.RemoteIp} was blocked due to being a reserved address (bogon)", LogLevel.Warning);
                    return true;
                }

                switch (abuseConfidenceScore)
                {
                    case >= 25:
                        ServerSetup.Logger("---------Lobby-Server---------");
                        var comment = $"{client.RemoteIp} was blocked with a score of {abuseConfidenceScore}";
                        ServerSetup.Logger(comment, LogLevel.Warning);
                        ReportEndpoint(client, comment);
                        return true;
                    case >= 0:
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.Logger($"{client.RemoteIp} - API Issue, confidence score was null");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.Logger($"{client.RemoteIp} - API Issue, response was not successful");
                return false;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger("Unknown issue with IPDB, connections refused", LogLevel.Warning);
            ServerSetup.Logger($"{ex}");
            Crashes.TrackError(ex);
            return true;
        }

        return true;
    }

    private void ReportEndpoint(ISocketClient client, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        var request = new RestRequest("", Method.Post);
        request.AddHeader("Key", keyCode);
        request.AddHeader("Accept", "application/json");
        request.AddParameter("ip", client.RemoteIp.ToString());
        request.AddParameter("categories", "14, 15, 16, 21");
        request.AddParameter("comment", comment);
        _restClient.Execute(request);
    }

    private readonly HashSet<string> _bannedIPs = new();

    private bool BannedIpCheck(ISocketClient client, string ip)
    {
        if (client.Socket.RemoteEndPoint == null || ip == null) return true;

        // Add banned player IPs to the _bannedIPs HashSet
        _bannedIPs.Add("0.0.0.0");

        return _bannedIPs.Contains(ip);
    }

    #endregion
}