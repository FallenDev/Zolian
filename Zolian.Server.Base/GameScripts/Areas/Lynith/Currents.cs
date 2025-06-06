﻿using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;
using System.Numerics;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("Currents")]
public class Currents : AreaScript
{
    public Currents(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        switch (newLocation.X)
        {
            case 0 when newLocation.Y == 21:
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aCurrents keep pushing you back..");
                break;
        }

        if (client.Aisling.EquipmentManager.Equipment[16]?.Item?.Template.Name == "Scuba Gear") return;
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
        if (client.Aisling.EquipmentManager.Equipment[16]?.Item?.Template.Name == "Scuba Gear") return;
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
        if (client.Aisling.EquipmentManager.Equipment[16]?.Item?.Template.Name == "Scuba Gear") return;
        if (client.Aisling.Race.RaceFlagIsSet(Race.Merfolk)) return;
        var drownTick = client.Aisling.MaximumHp * 0.05;
        client.Aisling.CurrentHp -= (long)drownTick;
        client.SendAttributes(StatUpdateType.Vitality);

        if (!(client.Aisling.CurrentHp <= client.Aisling.MaximumHp * 0.10)) return;
        var drown = new DebuffReaping();
        drown.OnApplied(client.Aisling, drown);
    }
}