using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("BlackBeard")]
public class BlackBeard : AreaScript
{
    public BlackBeard(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case 4 when newLocation.Y == 1:
                var key = client.Aisling.HasItemReturnItem("Ship Keys");
                if (key == null)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Door is locked!");
                    return;
                }

                client.Aisling.Inventory.RemoveRange(client, key, 1);
                client.TransitionToMap(6630, new Position(10, 18));
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}