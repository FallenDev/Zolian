using System.Net.Sockets;
using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Network.Client.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Darkages.Network.Client
{
    public class LobbyClient : SocketClientBase, ILobbyClient
    {
        private readonly ILobbyServer<LobbyClient> _server;

        public LobbyClient([NotNull]ILobbyServer<LobbyClient> server, [NotNull] Socket socket, [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer, 
            [NotNull] [ItemNotNull] ILogger<SocketClientBase> logger) : base(socket, crypto, packetSerializer, logger)
        {
            _server = server;
        }

        protected override ValueTask HandlePacketAsync(Span<byte> span)
        {
            var opCode = span[3];
            var isEncrypted = Crypto.ShouldBeEncrypted(opCode);
            var packet = new ClientPacket(ref span, isEncrypted);

            if (isEncrypted)
                Crypto.Decrypt(ref packet);

            return _server.HandlePacketAsync(this, in packet);
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
    }
}
