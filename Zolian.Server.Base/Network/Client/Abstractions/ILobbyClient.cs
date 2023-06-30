using Chaos.Networking.Abstractions;

namespace Darkages.Network.Client.Abstractions;

public interface ILobbyClient : ISocketClient
{
    void SendConnectionInfo(uint serverTableCheckSum);
    void SendServerTable(byte[] serverTableData);
}