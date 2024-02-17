using Chaos.Common.Definitions;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Abel;

[Script("Dark District")]
public class DarkDistrict : AreaScript
{
    public DarkDistrict(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, "You feel a presence nearby.");
        if (client.Aisling.QuestManager.Keela != 1 || !client.Aisling.QuestManager.KeelaQuesting) return;
        if (client.Aisling.HasItem("Assassin Notes")) return;
        client.SendServerMessage(ServerMessageType.ActiveMessage, "An assassin briefly appears and tucks some notes in your pocket.");
        client.GiveItem("Assassin Notes");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}