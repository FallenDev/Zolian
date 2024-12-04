using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Generic;

[Script("Arena Entrance")]
public class ArenaEntrance : AreaScript
{
    public ArenaEntrance(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        base.OnMapClick(client, x, y);
        var arenaBoardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(3, out var arenaBoard);
        var trashBoardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(4, out var trashBoard);

        switch (x)
        {
            case 8 when y == 3:
            case 9 when y == 4:
                if (arenaBoardFound)
                    client.SendBoard(arenaBoard);
                break;
            case 2 when y == 3:
            case 3 when y == 4:
                if (trashBoardFound)
                    client.SendBoard(trashBoard);
                break;
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case 13 when newLocation.Y == 7:
            case 13 when newLocation.Y == 8:
            case 13 when newLocation.Y == 9:
            case 13 when newLocation.Y == 10:
                var npc = ServerSetup.Instance.GlobalMundaneCache.Values.First(npc => npc.Name == "Arena Host");
                var script = npc.Scripts.Values.FirstOrDefault();
                script?.OnClick(client, npc.Serial);
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}