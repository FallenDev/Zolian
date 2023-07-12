using System.Net.Sockets;
using Chaos.Networking.Abstractions;

namespace Darkages.Interfaces
{
    public interface IClientFactory<out T> where T : SocketClientBase
    {
        T CreateClient(Socket socket);
    }
}
