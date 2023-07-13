using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("MilethTrainingCenter")]
public class MilethTrainingCenter : AreaScript
{
    public MilethTrainingCenter(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        if ((x != 4 || y != 0) && (x != 5 || y != 1)) return;
        client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        client.SendStats(StatusFlags.Health);
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(209, client.Aisling.Pos));
        client.SendServerMessage(ServerMessageType.OrangeBar1, "Ahh Refreshing!");
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}