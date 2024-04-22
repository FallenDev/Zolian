using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

using Darkages.Interfaces;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Microsoft.Extensions.Logging;
using RestSharp;

using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using ServiceStack;
using ConnectionInfo = Chaos.Networking.Options.ConnectionInfo;
using ServerOptions = Chaos.Networking.Options.ServerOptions;

namespace Darkages.Network.Server;

/// <summary>
/// Connections to the server enter here, in order
///     -> OnConnection (Establishes the connection and checks IP)
///     -> OnVersion (Checks version of client)
///     -> OnServerTableRequest (Sends server table)
/// </summary>
[UsedImplicitly]
public sealed class LobbyServer : ServerBase<ILobbyClient>, ILobbyServer<ILobbyClient>
{
    private readonly IClientFactory<LobbyClient> _clientProvider;
    private readonly MServerTable _serverTable;
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
                    var redirect = new Chaos.Networking.Entities.Redirect(EphemeralRandomIdGenerator<uint>.Shared.NextId,
                        new ConnectionInfo { Address = connectInfo.Address, Port = connectInfo.Port },
                        ServerType.Login,
                        localClient.Crypto.Key,
                        localClient.Crypto.Seed);

                    RedirectManager.Add(redirect);
                    ServerSetup.ConnectionLogger($"Redirecting {client.RemoteIp} to Login Server");
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
        var handler = ClientHandlers[(byte)opCode];

        try
        {
            if (handler is not null) return handler(client, in packet);
            ServerSetup.PacketLogger($"Unknown message to lobby server with code {opCode} from {client.RemoteIp}");
            Crashes.TrackError(new Exception($"Unknown message to lobby server with code {opCode} from {client.RemoteIp}"));
        }
        catch
        {
            // ignored
        }

        return default;
    }

    protected override void IndexHandlers()
    {
        base.IndexHandlers();
        ClientHandlers[(byte)ClientOpCode.Version] = OnVersion;
        ClientHandlers[(byte)ClientOpCode.ServerTableRequest] = OnServerTableRequest;
    }

    protected override void OnConnected(Socket clientSocket)
    {
        ServerSetup.ConnectionLogger($"Lobby connection from {clientSocket.RemoteEndPoint as IPEndPoint}");

        if (clientSocket.RemoteEndPoint is not IPEndPoint ip)
        {
            ServerSetup.ConnectionLogger("Socket not a valid endpoint");
            return;
        }

        var ipAddress = ip.Address;
        var client = _clientProvider.CreateClient(clientSocket);
        client.OnDisconnected += OnDisconnect;
        var safe = false;

        foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp => savedIp == ipAddress.ToString()))
            safe = true;

        if (!safe)
        {
            var badActor = ClientOnBlackList(ipAddress.ToString());
            if (badActor)
            {
                try
                {
                    client.Disconnect();
                }
                catch
                {
                    // ignored
                }
                return;
            }
        }

        if (!ClientRegistry.TryAdd(client))
        {
            ServerSetup.ConnectionLogger("Two clients ended up with the same id - newest client disconnected");
            try
            {
                client.Disconnect();
            }
            catch
            {
                // ignored
            }
            return;
        }

        ServerSetup.Instance.GlobalLobbyConnection.TryAdd(ipAddress, ipAddress);
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
    private bool ClientOnBlackList(string remoteIp)
    {
        if (remoteIp.IsNullOrEmpty()) return true;

        switch (remoteIp)
        {
            case "127.0.0.1":
            case InternalIP:
                return false;
        }

        var bogonCheck = BannedIpCheck(remoteIp);
        if (bogonCheck)
        {
            ServerSetup.ConnectionLogger("-----------------------------------");
            ServerSetup.ConnectionLogger($"{remoteIp} is banned and unable to connect");
            return true;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", remoteIp);
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = ServerSetup.Instance.RestClient.Execute<Ipdb>(request);

            if (response.IsSuccessful)
            {
                var json = response.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response is null or length is 0");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                Analytics.TrackEvent($"{remoteIp} has a confidence score of {abuseConfidenceScore}, is using tor: {tor}, and IP type: {usageType}");

                if (tor == true)
                {
                    ServerSetup.ConnectionLogger("---------Lobby-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} is using tor and automatically blocked", LogLevel.Warning);
                    return true;
                }

                if (usageType == "Reserved")
                {
                    ServerSetup.ConnectionLogger("---------Lobby-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} was blocked due to being a reserved address (bogon)", LogLevel.Warning);
                    return true;
                }

                switch (abuseConfidenceScore)
                {
                    case >= 5:
                        ServerSetup.ConnectionLogger("---------Lobby-Server---------");
                        var comment = $"{remoteIp} has been blocked due to a high risk assessment score of {abuseConfidenceScore}, indicating a recognized malicious entity.";
                        ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
                        ReportEndpoint(remoteIp, comment);
                        return true;
                    case >= 0:
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, confidence score was null");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response was not successful");
                return false;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger("Unknown issue with IPDB, connections refused", LogLevel.Warning);
            ServerSetup.ConnectionLogger($"{ex}");
            Crashes.TrackError(ex);
            return false;
        }

        return true;
    }

    private void ReportEndpoint(string remoteIp, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", "14, 15, 16, 21");
            request.AddParameter("comment", comment);
            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (!response.IsSuccessful)
            {
                ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            }
        }
        catch
        {
            // ignore
        }
    }

    private readonly HashSet<string> _bannedIPs = new();

    private bool BannedIpCheck(string ip)
    {
        if (ip.IsNullOrEmpty()) return true;

        // Add banned player IPs to the _bannedIPs HashSet
        _bannedIPs.Add("0.0.0.0");

        return _bannedIPs.Contains(ip);
    }

    #endregion
}