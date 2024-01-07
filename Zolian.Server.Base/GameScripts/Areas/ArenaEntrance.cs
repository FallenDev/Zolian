using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas;

[Script("Arena Entrance")]
public class ArenaEntrance : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();
    public ArenaEntrance(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        var arenaBoardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(2, out var arenaBoard);
        var trashBoardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(3, out var trashBoard);

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
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        switch (newLocation.X)
        {
            case 13 when newLocation.Y == 7:
            case 13 when newLocation.Y == 8:
            case 13 when newLocation.Y == 9:
            case 13 when newLocation.Y == 10:
                var npc = ServerSetup.Instance.GlobalMundaneCache.Values.First(npc => npc.Name == "Arena Host");
                var script = npc.Scripts.Values.First();
                script.OnClick(client, npc.Serial);
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}