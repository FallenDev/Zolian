using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("ToTheBrig")]
public class ToTheBrig : AreaScript
{
    public ToTheBrig(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case > 12:
                if (client.Aisling.EquipmentManager.Equipment[16]?.Item == null || !client.Aisling.EquipmentManager.Equipment[16].Item.Template.Name.Contains("Pirate"))
                {
                    client.TransitionToMap(6629, new Position(30, 15));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bARRRRGHH! To the brig with ye!");
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}