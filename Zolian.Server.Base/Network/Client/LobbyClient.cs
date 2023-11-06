using Chaos.Common.Definitions;
using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Darkages.Network.Client.Abstractions;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using System.Net.Sockets;

namespace Darkages.Network.Client
{
    [UsedImplicitly]
    public class LobbyClient([NotNull] ILobbyServer<LobbyClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
            [NotNull] ILogger<SocketClientBase> logger)
        : SocketClientBase(socket, crypto, packetSerializer, logger), ILobbyClient
    {
        protected override ValueTask HandlePacketAsync(Span<byte> span)
        {
            var opCode = span[3];
            var isEncrypted = Crypto.ShouldBeEncrypted(opCode);
            var packet = new ClientPacket(ref span, isEncrypted);

            if (isEncrypted)
                Crypto.Decrypt(ref packet);

            return server.HandlePacketAsync(this, in packet);
        }

        public void SendServerTable(byte[] serverTableData)
        {
            var args = new ServerTableArgs
            {
                ServerTable = serverTableData
            };

            Send(args);
        }

        public void SendConnectionInfo(uint serverTableCheckSum)
        {
            var args = new ConnectionInfoArgs
            {
                Key = Crypto.Key,
                Seed = Crypto.Seed,
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
}
