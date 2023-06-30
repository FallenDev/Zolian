using System.Net;
using System.Net.Sockets;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;
using Darkages.Meta;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darkages.Network.Server;

public sealed class LobbyServer : ServerBase<ILobbyClient>, ILobbyServer<ILobbyClient>
{
    private readonly IClientFactory<LobbyClient> ClientProvider;
    private readonly MServerTable ServerTable;

    public LobbyServer(
        IClientFactory<LobbyClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<ILobbyClient> lobbyRegistry,
        ILogger<LobbyServer> logger,
        IOptions<ServerOptions> options
    ) : base(redirectManager, packetSerializer, lobbyRegistry, options, logger)
    {
        ClientProvider = clientProvider;
        ServerTable  = MServerTable.FromFile("MServerTable.xml");
        IndexHandlers();
    }

    #region OnHandlers
    public ValueTask OnConnectionInfoRequest(ILobbyClient client, in ClientPacket _)
    {
        ValueTask InnerOnConnectionInfoRequest(ILobbyClient localClient)
        {
            localClient.SendConnectionInfo(ServerTable.Hash);

            return default;
        }

        return ExecuteHandler(client, InnerOnConnectionInfoRequest);
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
                    var connectInfo = new IPEndPoint(ServerTable.Servers[0].Address, ServerTable.Servers[0].Port);
                    var redirect = new Redirect(EphemeralRandomIdGenerator<uint>.Shared.NextId, new ConnectionInfo{Address = connectInfo.Address, Port = connectInfo.Port}, 
                        ServerType.Lobby, localClient.Crypto.Key, localClient.Crypto.Seed, $"socket[{localClient.Id}]");
                    localClient.SendRedirect(redirect);
                    break;
                case ServerTableRequestType.RequestTable:
                    localClient.SendServerTable(ServerTable.Data);
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

        ClientHandlers[(byte)ClientOpCode.ConnectionInfoRequest] = OnConnectionInfoRequest;
        ClientHandlers[(byte)ClientOpCode.ServerTableRequest] = OnServerTableRequest;
    }

    protected override void OnConnection(IAsyncResult ar)
    {
        var serverSocket = (Socket)ar.AsyncState!;
        var clientSocket = serverSocket.EndAccept(ar);
        // ToDo Copy over OldLoginServer "ClientConnected"



        serverSocket.BeginAccept(OnConnection, serverSocket);
        var client = ClientProvider.CreateClient(clientSocket);
        client.BeginReceive();
        client.SendAcceptConnection();
    }

    private void OnDisconnect(object? sender, EventArgs e)
    {
        var client = (ILobbyClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }
    #endregion
}