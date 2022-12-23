namespace Darkages.Network.GameServer;

public abstract class GameServerComponent
{
    protected GameServerComponent(Server.GameServer server) => Server = server;
    protected Server.GameServer Server { get; }
    protected internal abstract void Update(TimeSpan elapsedTime);
}
