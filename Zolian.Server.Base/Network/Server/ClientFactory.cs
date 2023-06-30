using System.Net.Sockets;
using Chaos.Networking.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Darkages.Network.Server
{
    public class ClientFactory<T> : IClientFactory<T> where T : SocketClientBase
    {
        private readonly IServiceProvider serviceProvider;

        public ClientFactory(IServiceProvider service)
        {
            serviceProvider = service;
        }

        public T CreateClient(Socket socket)
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, socket);
        }
    }
}
