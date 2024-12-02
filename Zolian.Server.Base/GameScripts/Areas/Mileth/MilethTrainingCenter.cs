using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("MilethTrainingCenter")]
public class MilethTrainingCenter : AreaScript
{
    public MilethTrainingCenter(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        base.OnMapClick(client, x, y);

        if ((x != 4 || y != 0) && (x != 5 || y != 1)) return;
        client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        client.SendAttributes(StatUpdateType.FullVitality);
        client.Aisling.SendAnimationNearby(209, new Position(client.Aisling.Pos));
        client.SendServerMessage(ServerMessageType.OrangeBar1, "Ahh Refreshing!");
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}