using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("South Lynith")]
public class SouthLynith : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public SouthLynith(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        switch (newLocation.X)
        {
            case 6 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(61, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(61, 1));
                break;
            case 7 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(63, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(63, 1));
                break;
            case 8 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(64, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(64, 1));
                break;
            case 9 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(65, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(65, 1));
                break;
            case 10 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(66, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(66, 1));
                break;
            case 11 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(67, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(67, 1));
                break;
            case 12 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(68, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(68, 1));
                break;
            case 13 when newLocation.Y == 19:
                if (client.Aisling.QuestManager.PirateShipAccess)
                {
                    client.TransitionToMap(6637, new Position(69, 1));
                    return;
                }

                client.TransitionToMap(6627, new Position(69, 1));
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}