using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Darkages.Meta;
using Darkages.Network.Server;
using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using System.Net.Sockets;
using ILoginClient = Darkages.Network.Client.Abstractions.ILoginClient;

namespace Darkages.Network.Client;

[UsedImplicitly]
public class LoginClient([NotNull] ILoginServer<ILoginClient> server, [NotNull] Socket socket,
        [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
        [NotNull] ILogger<LoginClient> logger)
    : LoginClientBase(socket, crypto, packetSerializer, logger), ILoginClient
{
    protected override ValueTask HandlePacketAsync(Span<byte> span)
    {
        var opCode = span[3];
        var packet = new Packet(ref span, Crypto.IsClientEncrypted(opCode));

        if (packet.IsEncrypted)
            Crypto.Decrypt(ref packet);

        return server.HandlePacketAsync(this, in packet);
    }

    public void SendLoginControl(LoginControlsType loginControlsType, string message)
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

    public void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, [CanBeNull] string name = null)
    {
        var args = new MetaDataArgs
        {
            MetaDataRequestType = metaDataRequestType
        };

        switch (metaDataRequestType)
        {
            case MetaDataRequestType.DataByName:
            {
                try
                {
                    var metaData = ServerSetup.Instance.Game.Metafiles.Values.FirstOrDefault(file => file.Name == name);
                    if (metaData == null) break;
                    args.MetaDataInfo = new MetaDataInfo
                    {
                        Name = metaData.Name,
                        Data = metaData.DeflatedData,
                        CheckSum = metaData.Hash
                    };
                }
                catch (Exception ex)
                {
                    ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
                    ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
                    SentrySdk.CaptureException(ex);
                }
                
                break;
            }
            case MetaDataRequestType.AllCheckSums:
            {
                try
                {
                    args.MetaDataCollection = [];
                    foreach (var file in ServerSetup.Instance.Game.Metafiles.Values.Where(file => !file.Name.Contains("SClass")))
                    {
                        var metafileInfo = new MetaDataInfo
                        {
                            CheckSum = file.Hash,
                            Data = file.DeflatedData,
                            Name = file.Name
                        };

                        args.MetaDataCollection?.Add(metafileInfo);
                    }
                }
                catch (Exception ex)
                {
                    ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
                    ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
                    SentrySdk.CaptureException(ex);
                }
                
                break;
            }
        }

        Send(args);
    }
}