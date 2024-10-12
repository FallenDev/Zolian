using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Object;

namespace Darkages.ScriptingBase;

public abstract class GlobalScript(WorldClient client) : ObjectManager
{
    private WorldClient _client = client;
    public WorldServerTimer Timer { get; set; }
    public abstract void Update(TimeSpan elapsedTime);
}