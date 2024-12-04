using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("EscapeBrig")]
public class EscapeBrig : AreaScript
{
    public EscapeBrig(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case <= 1:
                var key = client.Aisling.HasItemReturnItem("Brig Key");
                if (key == null)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Locked!");
                    return;
                }

                client.Aisling.Inventory.RemoveRange(client, key, 1);
                client.TransitionToMap(6179, new Position(5, 11));
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}