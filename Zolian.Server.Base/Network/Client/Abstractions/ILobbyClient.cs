using Chaos.Networking.Abstractions;

namespace Darkages.Network.Client.Abstractions;

public interface ILobbyClient : IConnectedClient
{
    void SendConnectionInfo(ushort port);
    void SendLoginMessage(LoginMessageType loginMessageType, string message = null);
}