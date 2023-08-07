namespace Darkages.Network.Server;

public abstract class WorldServerComponent
{
    protected WorldServerComponent(WorldServer server) => Server = ServerSetup.Instance.Game;
    protected static WorldServer Server { get; private set; }
    protected internal abstract void Update(TimeSpan elapsedTime);
}
