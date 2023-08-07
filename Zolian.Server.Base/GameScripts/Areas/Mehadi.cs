using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Mehadi")]
public class Mehadi : AreaScript
{
    public Mehadi(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        if (client == null) return;
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

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        if (client == null) return;
        if (client.Aisling.QuestManager.SwampAccess) return;
        if (client.Aisling.Map.ID == 3071) return;

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