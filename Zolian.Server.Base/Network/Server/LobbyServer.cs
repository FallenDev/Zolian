using Chaos.Networking.Abstractions;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Meta;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Common;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JetBrains.Annotations;
using ServiceStack;
using ConnectionInfo = Chaos.Networking.Options.ConnectionInfo;
using ServerOptions = Chaos.Networking.Options.ServerOptions;
using ILobbyClient = Darkages.Network.Client.Abstractions.ILobbyClient;

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

    public ValueTask OnVersion(ILobbyClient client, in Packet packet)
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

    public ValueTask OnServerTableRequest(ILobbyClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<ServerTableRequestArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnServerTableRequest);

        ValueTask InnerOnServerTableRequest(ILobbyClient localClient, ServerTableRequestArgs localArgs)
        {
            switch (localArgs.ServerTableRequestType)
            {
                case ServerTableRequestType.ServerId:
                    var connectInfo = new IPEndPoint(_serverTable.Servers[0].Address, _serverTable.Servers[0].Port);
                    var redirect = new Chaos.Networking.Entities.Redirect(
                        EphemeralRandomIdGenerator<uint>.Shared.NextId,
                        new ConnectionInfo { Address = connectInfo.Address, Port = connectInfo.Port },
                        ServerType.Login,
                        Encoding.ASCII.GetString(client.Crypto.Key),
                        localClient.Crypto.Seed);

                    RedirectManager.Add(redirect);
                    ServerSetup.ConnectionLogger($"Redirecting {client.RemoteIp} to Login Server");
                    localClient.SendRedirect(redirect);
                    break;
                case ServerTableRequestType.RequestTable:
                    localClient.SendServerTableResponse(_serverTable.Data);
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

    public override ValueTask HandlePacketAsync(ILobbyClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[opCode];

        try
        {
            if (handler is not null) return handler(client, in packet);
            ServerSetup.PacketLogger("//////////////// Handled Lobby Server Unknown Packet ////////////////", LogLevel.Error);
            ServerSetup.PacketLogger($"{opCode} from {client.RemoteIp}", LogLevel.Error);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(new Exception($"Unknown packet {opCode} from {client.RemoteIp} on LobbyServer \n {ex}"));
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

        var banned = BannedIpCheck(ipAddress.ToString());
        if (banned)
        {
            client.Disconnect();
            ServerSetup.ConnectionLogger($"Banned connection attempt from {ip}");
            return;
        }

        foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp => savedIp == ipAddress.ToString()))
            safe = true;

        if (!safe)
        {
            var isBadActor = Task.Run(() => BadActor.ClientOnBlackListAsync(ipAddress.ToString())).Result;

            if (isBadActor)
            {
                try
                {
                    client.Disconnect();
                    ServerSetup.ConnectionLogger($"Disconnected Bad Actor from {ip}");
                }
                catch
                {
                    // ignored
                }

                return;
            }

            ServerSetup.ConnectionLogger($"Good Actor! {ip}");
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
        client.SendAcceptConnection("CONNECTED SERVER");
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
        var client = (ILobbyClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    private readonly HashSet<string> _bannedIPs = [];

    private bool BannedIpCheck(string ip)
    {
        if (ip.IsNullOrEmpty()) return true;

        // Add banned player IPs to the _bannedIPs HashSet
        _bannedIPs.Add("0.0.0.0");

        return _bannedIPs.Contains(ip);
    }

    #endregion
}