using Chaos.Networking.Abstractions;

using System.Net.Sockets;

namespace Darkages.Interfaces
{
    public interface IClientFactory<out T> where T : SocketClientBase
    {
        T CreateClient(Socket socket);
    }
}
