using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.GameScripts.Areas.VoidSphere;

[Script("VoidSphereDivide")]
public class VoidSphereDivide : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public VoidSphereDivide(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
        var debuff = new DebuffReaping();

        if (client.Aisling.EquipmentManager.Equipment[18]?.Item.Template.Name == "Auto Spark") return;

        if (vectorMap.X is > 15 and < 21)
        {
            client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
            client.TransitionToMap(14757, new Position(13, 34));
            client.SendSound(0x9B, false);
        }

        if (!(vectorMap.Y > 35) && !(vectorMap.Y < 3) && !(vectorMap.X > 35) && !(vectorMap.X < 3)) return;
        client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
        client.TransitionToMap(14757, new Position(13, 34));
        client.SendSound(0x9B, false);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}