using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Object;

namespace Darkages.Scripting;

public abstract class GlobalScript : ObjectManager, IScriptBase
{
    private WorldClient _client;
    protected GlobalScript(WorldClient client) => _client = client;
    public WorldServerTimer Timer { get; set; }
    public abstract void Update(TimeSpan elapsedTime);
}