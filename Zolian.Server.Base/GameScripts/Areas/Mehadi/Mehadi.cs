using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Mehadi;

[Script("Mehadi")]
public class Mehadi : AreaScript
{
    public Mehadi(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        if (client.Aisling.QuestManager.SwampAccess) return;
        if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(3071, out var npcArray) || npcArray.Length == 0) return;
        if (!npcArray.TryGetValue<Mundane>(t => t.Name == "Shreek", out var mundane) || mundane == null) return;
        mundane.AIScript?.OnClick(client, mundane.Serial);

        client.TransitionToMap(3071, new Position(3, 7));
    }

    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        if (client == null) return;
        if (client.Aisling.QuestManager.SwampAccess) return;
        if (client.Aisling.Map.ID == 3071) return;
        if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(14759, out var npcArray) || npcArray.Length == 0) return;
        if (!npcArray.TryGetValue<Mundane>(t => t.Name == "Shreek The Mad", out var mundane) || mundane == null) return;
        mundane.AIScript?.OnClick(client, mundane.Serial);

        client.TransitionToMap(3071, new Position(3, 7));
    }
}