using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas.Abel;

[Script("Fight Hall")]
public class FightHall : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public FightHall(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime)
    {
        foreach (var player in _playersOnMap.Values)
        {
            if (player == null) return;
            if (player.Map.ID != 195) return;
            if (!player.IsDead()) return;
            player.Client.GhostFormToAisling();
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
        }
    }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (client.Aisling == null) return;
        if (client.Aisling.Map.ID != 195) return;
        if (!client.Aisling.IsDead()) return;
        client.GhostFormToAisling();
        client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
    }

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}