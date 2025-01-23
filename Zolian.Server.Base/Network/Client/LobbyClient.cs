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
        try
        {
            // Fully parse the Packet from the span
            var packet = new Packet(ref span);

            if (packet.Payload.Length == 0)
            {
                Logger.LogWarning("Received packet with empty payload. OpCode={OpCode}", packet.OpCode);
            }

            // Pass the fully constructed Packet to the server for handling
            return server.HandlePacketAsync(this, in packet);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing packet from span: {RawBuffer}", BitConverter.ToString(span.ToArray()));
            return default;
        }
    }

    public void SendConnectionInfo(ushort port)
    {
        var args = new ConnectionInfoArgs
        {
            PortNumber = port
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