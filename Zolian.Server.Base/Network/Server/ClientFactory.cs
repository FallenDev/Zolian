using System.Net.Sockets;
using Chaos.Networking.Abstractions;
using Darkages.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Darkages.Network.Server
{
    public class ClientFactory<T>(IServiceProvider service) : IClientFactory<T>
        where T : SocketClientBase
    {
        public T CreateClient(Socket socket)
        {
            return ActivatorUtilities.CreateInstance<T>(service, socket);
        }
    }
}
