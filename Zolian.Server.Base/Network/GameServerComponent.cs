using Darkages.Network.Server;

namespace Darkages.Network;

public abstract class WorldServerComponent
{
    protected WorldServerComponent(WorldServer server) => Server = ServerSetup.Instance.Game;
    protected static WorldServer Server { get; private set; }
    protected internal abstract void Update(TimeSpan elapsedTime);
}
