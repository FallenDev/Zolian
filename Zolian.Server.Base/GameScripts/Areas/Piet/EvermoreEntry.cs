using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Evermore Entry")]
public class EvermoreEntry : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public EvermoreEntry(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
        if (vectorMap != new Vector2(12, 16)) return;

        if (client.Aisling.QuestManager.AssassinsGuildReputation >= 1)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou may enter");
            client.TransitionToMap(286, new Position(1, 13));
        }
        else
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aThere is a bloody hand print on the back of the statue");
            Task.Delay(300).ContinueWith(c =>
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou are not worthy.. begone");
            });
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}