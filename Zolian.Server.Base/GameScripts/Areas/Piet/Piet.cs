using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Piet")]
public class Piet : AreaScript
{
    public Piet(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        if (x == 48 && y == 22 || x == 48 && y == 21 || x == 47 && y == 21)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "Door seems to be locked.");
        }
        else
        {
            base.OnMapClick(client, x, y);
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}