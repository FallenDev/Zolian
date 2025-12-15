using Chaos.Networking.Abstractions;
using System.Net.Sockets;

namespace Darkages.Network.Client.Abstractions
{
    public interface IClientFactory<out T> where T : SocketTransportBase
    {
        T CreateClient(Socket socket);
    }
}
