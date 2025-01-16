using Chaos.Common.Identity;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Meta;
using Darkages.Network.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Chaos.Networking.Abstractions.Definitions;
using JetBrains.Annotations;
using ServiceStack;
using ConnectionInfo = Chaos.Networking.Options.ConnectionInfo;
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
    private readonly MServerTable _serverTable;

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
                        null, 0);

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
            // Log OpCode and validate payload
            Logger.LogInformation("OpCode: {OpCode}, Sequence: {Sequence}, Payload Length: {PayloadLength}",
                packet.OpCode, packet.Sequence, packet.Payload.Length);

            var payload = packet.Payload;

            switch (packet.OpCode)
            {
                case 0x01: // String
                    Logger.LogInformation("Payload: {PayloadAscii}", Encoding.ASCII.GetString(payload.ToArray()));
                    client.SendLoginMessage(LoginMessageType.Confirm, "Hurray you have successfully logged on! Now let's throw a ton of string data at you to see if you can handle it!" +
                                                                      "Lorem ipsum odor amet, consectetuer adipiscing elit. Purus mi volutpat donec tellus fames dolor. Purus ac lobortis cras molestie luctus tristique elementum neque. Dictumst aptent ligula neque facilisi ultricies torquent? Bibendum rhoncus id bibendum odio euismod bibendum luctus. Atristique primis inceptos a posuere erat magna diam. Erat id facilisi fames himenaeos mattis mi arcu? Vehicula sociosqu facilisi dictumst sed interdum non eu.\n\nParturient class maximus, interdum hendrerit tristique ad at aliquet cursus. Magnis nibh lorem quis faucibus posuere et leo himenaeos inceptos. Gravida sit suspendisse efficitur lectus odio a quisque. Sollicitudin duis ornare ut primis nam. Curabitur donec cras tortor laoreet inceptos; ad posuere mauris magna. Diam luctus duis elementum sociosqu nec egestas habitant dapibus.\n\nVulputate commodo torquent duis elementum congue sapien libero fringilla. Erat dolor malesuada sagittis himenaeos pulvinar lorem faucibus tempus ornare. Nisl cras aliquet lacinia dapibus felis platea. Pulvinar per litora nam nunc phasellus vel fames. Natoque eleifend condimentum eget parturient dolor; suscipit odio odio. Nulla nullam accumsan eleifend gravida elit nec taciti tincidunt. Ac ut fringilla venenatis sed sociosqu.\n\nLeo auctor elementum odio nunc sodales venenatis adipiscing. Nunc pharetra proin urna posuere nec est. Non auctor mus mus integer senectus commodo taciti molestie. Mattis senectus faucibus nec semper ridiculus sed torquent odio. Elementum magna dui a, semper posuere orci morbi. Vehicula nascetur litora tempor aptent tortor dapibus nascetur pretium.\n\nElementum pellentesque purus elementum nunc adipiscing purus auctor integer laoreet. Ipsum primis sapien pretium fringilla purus; lacus enim. Erat praesent magnis malesuada iaculis maximus nullam? Pretium nunc porta finibus vel convallis. Diam sapien nunc massa a rutrum adipiscing posuere leo! Tellus magnis convallis eros platea placerat aliquam lobortis habitant! Sollicitudin scelerisque purus est sed quisque interdum.");


                    break;
                case 0x02: // Byte
                    Logger.LogInformation("Received Byte: {Value}", payload[0]);
                    break;
                case 0x03: // Int
                    Logger.LogInformation("Received Int: {Value}", BitConverter.ToInt32(payload));
                    break;
                case 0x04: // Long
                    Logger.LogInformation("Received Long: {Value}", BitConverter.ToInt64(payload));
                    break;
                case 0x05: // ULong
                    Logger.LogInformation("Received ULong: {Value}", BitConverter.ToUInt64(payload));
                    break;
                case 0x06: // Float
                    Logger.LogInformation("Received Float: {Value}", BitConverter.ToSingle(payload));
                    break;
                case 0x07: // Double
                    Logger.LogInformation("Received Double: {Value}", BitConverter.ToDouble(payload));
                    break;
                case 0x08: // Bool
                    Logger.LogInformation("Received Bool: {Value}", payload[0] == 1);
                    break;
                default:
                    Logger.LogWarning("Unknown OpCode: {OpCode}", packet.OpCode);
                    break;
            }

            // ToDo: Disable handler for now while we work on implementing new packet types
            //if (handler is not null) 
            //    return handler(client, in packet);

            // Log unknown packet
            //ServerSetup.ConnectionLogger($"{opCode} from {client.RemoteIp} - {packet.ToString()}", LogLevel.Error);
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
            client.SendAcceptConnection("CONNECTED SERVER");
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