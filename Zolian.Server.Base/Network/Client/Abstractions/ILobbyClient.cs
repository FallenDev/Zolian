using Chaos.Common.Definitions;
using Chaos.Networking.Abstractions;

namespace Darkages.Network.Client.Abstractions;

public interface ILobbyClient : IConnectedClient
{
    void SendConnectionInfo(uint serverTableCheckSum);
    void SendServerTableResponse(byte[] serverTableData);
    void SendLoginMessage(LoginMessageType loginMessageType, string message = null);
}