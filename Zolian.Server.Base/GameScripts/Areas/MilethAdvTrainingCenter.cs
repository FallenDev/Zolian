using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("MilethAdvTraining")]
public class MilethAdvTrainingCenter : AreaScript
{
    public MilethAdvTrainingCenter(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        switch (x)
        {
            case 20 when y == 1:
            case 19 when y == 0:
            case 11 when y == 1:
            case 10 when y == 0:
            case 9 when y == 14:
            case 8 when y == 13:
            case 17 when y == 14:
            case 16 when y == 13:
            case 26 when y == 29:
            case 25 when y == 28:
            case 1 when y == 23:
            case 0 when y == 22:
                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                client.SendAttributes(StatUpdateType.FullVitality);
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings,
                    c => c.SendAnimation(209, new Position(client.Aisling.Pos)));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Ahh Refreshing!");
                break;
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}