using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;

namespace Darkages.GameScripts.Areas;

[Script("EscapeBrig")]
public class EscapeBrig : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();
    public EscapeBrig(Area area) : base(area) => Area = area;
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
            case <= 1:
                var key = client.Aisling.HasItemReturnItem("Brig Key");
                if (key == null)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Locked!");
                    return;
                }
                
                client.Aisling.Inventory.RemoveRange(client, key, 1);
                client.TransitionToMap(6179, new Position(5, 11));
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}