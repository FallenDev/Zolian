using Chaos.Networking.Abstractions;

using Darkages.Meta;

namespace Darkages.Network.Client.Abstractions;

public interface ILobbyClient : ISocketClient
{
    void SendConnectionInfo(uint serverTableCheckSum);
    void SendServerTable(MServerTable serverTable);
}