using Chaos.Networking;
using Darkages.Network.Client.Abstractions;

namespace Darkages.Network.Client;

public sealed class WorldClientRegistry : ClientRegistry<IWorldClient>
{
    public override IEnumerator<IWorldClient> GetEnumerator() => Clients.Values.Where(c => c.Aisling != null).GetEnumerator();
}