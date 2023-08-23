using Chaos.Common.Definitions;
using Chaos.Networking.Abstractions;
using Darkages.Meta;

namespace Darkages.Network.Client.Abstractions;

public interface ILoginClient : ISocketClient
{
    void SendLoginControls(LoginControlsType loginControlsType, string message);
    void SendLoginMessage(LoginMessageType loginMessageType, string message = null);
    void SendLoginNotice(bool full, Notification notice);
    void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string name = null);
}