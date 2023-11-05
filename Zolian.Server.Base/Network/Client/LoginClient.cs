using Chaos.Common.Definitions;
using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Darkages.Meta;
using Darkages.Network.Client.Abstractions;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using System.Net.Sockets;

namespace Darkages.Network.Client
{
    public class LoginClient([NotNull] ILoginServer<LoginClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
            [NotNull][ItemNotNull] ILogger<SocketClientBase> logger)
        : SocketClientBase(socket, crypto, packetSerializer, logger), ILoginClient
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
                        args.MetaDataCollection = new List<MetaDataInfo>();
                        var metaFiles = metaDataStore.GetMetaFilesWithoutExtendedClasses();

                        foreach (var file in metaFiles)
                        {
                            var metafileInfo = new MetaDataInfo
                            {
                                CheckSum = file.Hash,
                                Data = file.DeflatedData,
                                Name = file.Name
                            };

                            args.MetaDataCollection.Add(metafileInfo);
                        }

                        break;
                    }
            }

            Send(args);
        }
    }
}
