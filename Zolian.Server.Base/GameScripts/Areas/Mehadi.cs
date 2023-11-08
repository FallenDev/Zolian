using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas;

[Script("Mehadi")]
public class Mehadi : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();
    public Mehadi(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (client.Aisling.QuestManager.SwampAccess) return;

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
        {
            if (npc.Value.Scripts is null) continue;
            if (npc.Value.Scripts.TryGetValue("Shreek", out var scriptObj))
            {
                scriptObj.OnClick(client, npc.Value.Serial);
            }
        }

        client.TransitionToMap(3071, new Position(3, 7));
    }

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        if (client == null) return;
        if (client.Aisling.QuestManager.SwampAccess) return;
        if (client.Aisling.Map.ID == 3071) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
        {
            if (npc.Value.Scripts is null) continue;
            if (npc.Value.Scripts.TryGetValue("Shreek Warn", out var scriptObj))
            {
                scriptObj.OnClick(client, npc.Value.Serial);
            }
        }

        client.TransitionToMap(3071, new Position(3, 7));
    }
}