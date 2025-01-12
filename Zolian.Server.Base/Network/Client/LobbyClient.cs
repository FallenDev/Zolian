using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using System.Net.Sockets;
using Darkages.Network.Server;
using ILobbyClient = Darkages.Network.Client.Abstractions.ILobbyClient;

namespace Darkages.Network.Client;

[UsedImplicitly]
public class LobbyClient([NotNull] ILobbyServer<ILobbyClient> server, [NotNull] Socket socket,
        [NotNull] IPacketSerializer packetSerializer,
        [NotNull] ILogger<LobbyClient> logger)
    : LobbyClientBase(socket, packetSerializer, logger), ILobbyClient
{
    protected override ValueTask HandlePacketAsync(Span<byte> span)
    {
        var opCode = span[3];
        var packet = new Packet(opCode);
        return server.HandlePacketAsync(this, in packet);
    }

    public void SendServerTableResponse(byte[] serverTableData)
    {
        var args = new ServerTableResponseArgs
        {
            ServerTable = serverTableData
        };

        Send(args);
    }

    public void SendConnectionInfo(uint serverTableCheckSum)
    {
        var args = new ConnectionInfoArgs
        {
            TableCheckSum = serverTableCheckSum
        };

        Send(args);
    }

    public void SendLoginMessage(LoginMessageType loginMessageType, [CanBeNull] string message = null)
    {
        var args = new LoginMessageArgs
        {
            LoginMessageType = loginMessageType,
            Message = message
        };

        Send(args);
    }
}