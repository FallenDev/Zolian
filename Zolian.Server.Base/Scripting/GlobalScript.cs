using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Object;

namespace Darkages.Scripting;

public abstract class GlobalScript : ObjectManager, IScriptBase
{
    private GameClient _client;
    protected GlobalScript(GameClient client) => _client = client;
    public GameServerTimer Timer { get; set; }
    public abstract void Update(TimeSpan elapsedTime);
}