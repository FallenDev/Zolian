using Darkages.Network.Server;

namespace Darkages.Network;

public abstract class GameServerComponent
{
    protected GameServerComponent(WorldServer server) => Server = server;
    protected static WorldServer Server { get; private set; }
    protected internal abstract void Update(TimeSpan elapsedTime);
}
