using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Dark District")]
public class DarkDistrict : AreaScript
{
    public DarkDistrict(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(GameClient client)
    {
        client.SendMessage(0x03, "You feel a presence nearby.");
        if (client.Aisling.QuestManager.Keela != 1 || !client.Aisling.QuestManager.KeelaQuesting) return;
        if (client.Aisling.HasItem("Assassin Notes")) return;
        client.SendMessage(0x03, "An assassin briefly appears and tucks some notes in your pocket.");
        client.GiveItem("Assassin Notes");
    }

    public override void OnMapExit(GameClient client) { }
    public override void OnMapClick(GameClient client, int x, int y) { }
    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(GameClient client, string message) { }
}