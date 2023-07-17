using System.Numerics;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Arena Entrance")]
public class ArenaEntrance : AreaScript
{
    private Sprite _aisling;

    public ArenaEntrance(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        switch (x)
        {
            case 8 when y == 3:
            case 9 when y == 4:
                client.SendBoard("Arena Updates");
                break;
            case 2 when y == 3:
            case 3 when y == 4:
                client.SendBoard("Trash Talk");
                break;
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (_aisling.Pos != vectorMap) return;
        switch (newLocation.X)
        {
            case 13 when newLocation.Y == 7:
            case 13 when newLocation.Y == 8:
            case 13 when newLocation.Y == 9:
            case 13 when newLocation.Y == 10:
                foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                {
                    if (npc.Value.Scripts is null) continue;
                    if (npc.Value.Scripts.TryGetValue("Arena Host", out var scriptObj))
                    {
                        scriptObj.OnClick(client, npc.Value.Serial);
                    }
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}