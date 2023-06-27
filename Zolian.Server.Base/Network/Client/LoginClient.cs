using System.Net.Sockets;
using Chaos.Common.Definitions;
using Chaos.Extensions.Networking;
using Chaos.Cryptography.Abstractions;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Meta;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Darkages.Network.Client
{
    public class LoginClient : SocketClientBase
    {
        private readonly ILoginServer<LoginClient> _server;

        public LoginClient([NotNull] ILoginServer<LoginClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
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


        public void SendLoginControls(LoginControlsType loginControlsType, string message)
        {
            var args = new LoginControlArgs
            {
                LoginControlsType = loginControlsType,
                Message = message
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

        public void SendLoginNotice(bool full, Notification notice)
        {
            var args = new LoginNoticeArgs
            {
                IsFullResponse = full
            };

            if (full)
                args.Data = notice.Data;
            else
                args.CheckSum = notice.Hash;
            
            Send(args);
        }

        public void SendMetaData(MetaDataRequestType metaDataRequestType, [NotNull] MetafileManager metaDataStore,
            [CanBeNull] string name = null)
        {
            var args = new MetaDataArgs
            {
                MetaDataRequestType = metaDataRequestType
            };

            switch (metaDataRequestType)
            {
                case MetaDataRequestType.DataByName:
                {
                    ArgumentNullException.ThrowIfNull(name);
                    var metaData = metaDataStore.GetMetaFile(name);
                    args.MetaDataInfo = new MetaDataInfo
                    {
                        Name = metaData.Name,
                        Data = metaData.DeflatedData,
                        CheckSum = metaData.Hash
                    };
                    break;
                }
                case MetaDataRequestType.AllCheckSums:
                {
                    args.MetaDataCollection = metaDataStore.GetMetaFiles().Select(i => new MetaDataInfo
                    {
                        Name = i.Name,
                        Data = i.DeflatedData,
                        CheckSum = i.Hash
                    }).ToList();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(metaDataRequestType), metaDataRequestType,
                        "Unknown enum value");
            }

            Send(args);
        }
    }
}
