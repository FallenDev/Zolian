using Chaos.Common.Definitions;
using Chaos.Networking.Abstractions;

namespace Darkages.Network.Client.Abstractions;

public interface ILobbyClient : ISocketClient
{
    void SendConnectionInfo(uint serverTableCheckSum);
    void SendServerTable(byte[] serverTableData);
    void SendLoginMessage(LoginMessageType loginMessageType, string message = null);
}