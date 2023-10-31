﻿using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Training Dummy")]
public class TrainingDummy : MonsterScript
{
    private DmgTable _incoming;

    public TrainingDummy(Monster monster, Area map) : base(monster, map)
    {
        Monster.BonusMr = 0;
        Monster.MonsterBank = new List<Item>();
    }

    public override void OnClick(WorldClient client)
    {
        var level = Monster.Template.Level.ToString();
        var ac = Monster.SealedAc.ToString();
        var defEle = ElementManager.ElementValue(Monster.DefenseElement);

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{Monster.Template.Name}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Lv:{level}, Ac:{ac}, Hp:{Monster.CurrentHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{Monster.Size} - {Monster.Template.MonsterType} - {Monster.MajorAttribute}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"-----------------------------");

    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        if (source is not Aisling aisling) return;
        _incoming.What = client.Aisling.ActionUsed;

        if (dmg > int.MaxValue)
        {
            dmg = int.MaxValue;
        }
            
        var convDmg = (int)dmg;
        _incoming.Damage = convDmg;
        var dmgDisplay = _incoming.Damage.ToString();

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}'s {_incoming.What}: {dmgDisplay} DMG.\n"));
        Monster.Facing((int)source.Pos.X, (int)source.Pos.Y, out var direction);

        if (!Monster.Position.IsNextTo(source.Position)) return;
        Monster.Direction = (byte)direction;
        Monster.Turn();
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var debuff in Monster.Debuffs.Values)
        {
            if (debuff != null)
                Monster.Debuffs.TryRemove(debuff.Name, out _);
        }

        foreach (var debuff in Monster.Buffs.Values)
        {
            if (debuff != null)
                Monster.Buffs.TryRemove(debuff.Name, out _);
        }

        Monster.BonusAc = 0;
    }

    public override void OnSkulled(WorldClient client) => client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(49, null, Monster.Serial));

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster.CurrentHp < Monster.MaximumHp)
            Monster.CurrentHp = Monster.MaximumHp;
    }

    private struct DmgTable
    {
        public int Damage { get; set; }
        public string What { get; set; }
    }
}