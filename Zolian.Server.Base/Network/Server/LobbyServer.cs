using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Network.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions.Definitions;
using JetBrains.Annotations;
using ServiceStack;
using ServerOptions = Chaos.Networking.Options.ServerOptions;
using ILobbyClient = Darkages.Network.Client.Abstractions.ILobbyClient;
using Darkages.Network.Client.Abstractions;

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

    public LobbyServer(
        IClientFactory<LobbyClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<ILobbyClient> lobbyRegistry,
        ILogger<LobbyServer> logger
        )
            : base(
                redirectManager,
                packetSerializer,
                lobbyRegistry,
                Microsoft.Extensions.Options.Options.Create(new ServerOptions
                {
                    Address = ServerSetup.Instance.IpAddress,
                    Port = ServerSetup.Instance.Config.LOBBY_PORT
                }),
                logger)
    {
        ServerSetup.Instance.LobbyServer = this;
        _clientProvider = clientProvider;
        IndexHandlers();
    }

    public ValueTask OnVersion(ILobbyClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<VersionArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnVersion);

        ValueTask InnerOnVersion(ILobbyClient localClient, VersionArgs localArgs)
        {
            if (!localArgs.Version.EqualsI(ServerSetup.Instance.Config.ClientVersion))
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "You're not using an authorized client. Please visit https://www.TheBuckNetwork.com/Zolian for the latest client.");
                localClient.Disconnect();
                return default;
            }

            localClient.SendConnectionInfo((ushort)ServerSetup.Instance.Config.LOGIN_PORT);
            return default;
        }
    }

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILobbyClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[opCode];

        try
        {
            if (handler is not null)
                return handler(client, in packet);

            // Log unknown packet
            ServerSetup.ConnectionLogger($"{opCode} from {client.RemoteIp} - {packet.ToString()}", LogLevel.Error);
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
    }

    protected override void OnConnected(Socket clientSocket)
    {
        ServerSetup.ConnectionLogger($"Lobby connection from {clientSocket.RemoteEndPoint as IPEndPoint}");

        if (clientSocket.RemoteEndPoint is not IPEndPoint ip)
        {
            ServerSetup.ConnectionLogger("Socket not a valid endpoint");
            return;
        }

        try
        {
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

            foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp =>
                         savedIp == ipAddress.ToString()))
                safe = true;

            if (!safe)
            {
                var badActor = BadActor.ClientOnBlackList(ipAddress.ToString());
                if (badActor)
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
            client.SendAcceptConnection("Lobby Connected");
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger($"Failed to authenticate lobbyServer using SSL/TLS. - {ex}");
        }
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