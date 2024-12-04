using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("BlackBeardQuarters")]
public class BlackBeardQuarters : AreaScript
{
    public BlackBeardQuarters(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case 12 when newLocation.Y == 2:
                if (!client.Aisling.QuestManager.ScubaSchematics && !client.Aisling.HasItem("Scuba Schematics"))
                {
                    var item = new Item();
                    item = item.Create(client.Aisling, "Scuba Schematics");
                    item.GiveTo(client.Aisling);
                    var item2 = new Item();
                    item2 = item2.Create(client.Aisling, "Hastily Written Notes");
                    item2.GiveTo(client.Aisling);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"Huh?! Some notes and {{=qSchematics");
                }

                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}