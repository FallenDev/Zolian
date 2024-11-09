using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Evermore")]
public class Evermore : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public Evermore(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        if (client.Aisling.Pos is not { X: 24, Y: 13 } || client.Aisling.QuestManager.AssassinsGuildReputation < 4) return;
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou've placed your hand on the bloodied writing.");
        client.TransitionToMap(289, new Position(55, 21));
        Task.Delay(2500).Wait();
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aAssassin: {{=bWelcome Friend... You have earned our favor.");
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}