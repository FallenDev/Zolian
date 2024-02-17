using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.GameScripts.Affects;

namespace Darkages.GameScripts.Areas.Generic;

[Script("UnderWater")]
public class UnderWater : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public UnderWater(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (client.Aisling.EquipmentManager.Equipment[16]?.Item.Template.Name == "Scuba Gear") return;
        if (client.Aisling.Race.RaceFlagIsSet(Race.Merfolk)) return;
        var drownTick = client.Aisling.MaximumHp * 0.05;
        client.Aisling.CurrentHp -= (long)drownTick;
        client.SendAttributes(StatUpdateType.Vitality);

        if (!(client.Aisling.CurrentHp <= client.Aisling.MaximumHp * 0.10)) return;
        var drown = new DebuffReaping();
        drown.OnApplied(client.Aisling, drown);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped)
    {
        if (client.Aisling.EquipmentManager.Equipment[16]?.Item.Template.Name == "Scuba Gear") return;
        if (client.Aisling.Race.RaceFlagIsSet(Race.Merfolk)) return;
        var drownTick = client.Aisling.MaximumHp * 0.05;
        client.Aisling.CurrentHp -= (long)drownTick;
        client.SendAttributes(StatUpdateType.Vitality);

        if (!(client.Aisling.CurrentHp <= client.Aisling.MaximumHp * 0.10)) return;
        var drown = new DebuffReaping();
        drown.OnApplied(client.Aisling, drown);
    }

    public override void OnGossip(WorldClient client, string message)
    {
        if (client.Aisling.EquipmentManager.Equipment[16]?.Item.Template.Name == "Scuba Gear") return;
        if (client.Aisling.Race.RaceFlagIsSet(Race.Merfolk)) return;
        var drownTick = client.Aisling.MaximumHp * 0.05;
        client.Aisling.CurrentHp -= (long)drownTick;
        client.SendAttributes(StatUpdateType.Vitality);

        if (!(client.Aisling.CurrentHp <= client.Aisling.MaximumHp * 0.10)) return;
        var drown = new DebuffReaping();
        drown.OnApplied(client.Aisling, drown);
    }
}