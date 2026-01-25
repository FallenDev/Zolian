using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Abel;

[Script("Atlantis")]
public class Atlantis : AreaScript
{
    public Atlantis(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        if (client.Aisling.Pos == new Vector2(31, 29) ||
            client.Aisling.Pos == new Vector2(30, 29) ||
            client.Aisling.Pos == new Vector2(30, 28) ||
            client.Aisling.Pos == new Vector2(31, 28))
        {
            if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(14759, out var npcArray) || npcArray.Length == 0) return;
            if (!npcArray.TryGetValue<Mundane>(t => t.Name == "Chromitus", out var mundane) || mundane == null) return;
            mundane.AIScript?.OnClick(client, mundane.Serial);
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