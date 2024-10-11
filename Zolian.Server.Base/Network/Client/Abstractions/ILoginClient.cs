using Chaos.Networking.Abstractions;

using Darkages.Meta;
using JetBrains.Annotations;

namespace Darkages.Network.Client.Abstractions;

public interface ILoginClient : IConnectedClient
{
    void SendLoginControl(LoginControlsType loginControlsType, string message);
    void SendLoginMessage(LoginMessageType loginMessageType, [CanBeNull] string message = null);
    void SendLoginNotice(bool full, Notification notice);
    void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string name = null);
}