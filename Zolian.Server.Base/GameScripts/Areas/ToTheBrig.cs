using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;

namespace Darkages.GameScripts.Areas;

[Script("ToTheBrig")]
public class ToTheBrig : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = new();
    public ToTheBrig(Area area) : base(area) => Area = area;
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
            case > 12:
                if (client.Aisling.EquipmentManager.Equipment[16]?.Item == null || !client.Aisling.EquipmentManager.Equipment[16].Item.Template.Name.Contains("Pirate"))
                {
                    client.TransitionToMap(6629, new Position(30, 15));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bARRRRGHH! To the brig with ye!");
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}