using Darkages.Network.Server;

namespace Darkages.Network;

public abstract class GameServerComponent
{
    protected GameServerComponent(GameServer server) => Server = server;
    protected GameServer Server { get; }
    protected internal abstract void Update(TimeSpan elapsedTime);
}
