using System.Numerics;

using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Necro Entrance")]
public class NecroEntrance : AreaScript
{
    private Aisling _aisling;

    public NecroEntrance(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (_aisling.Pos != vectorMap) return;
        switch (newLocation.X)
        {
            case 5 when newLocation.Y == 0:
            case 5 when newLocation.Y == 1:
                if (_aisling.QuestManager.TagorDungeonAccess)
                    client.TransitionToMap(1204, new Position(17, 37));
                else
                {
                    _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, null, _aisling.Serial));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou are forcibly repelled!");
                }

                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}