using System.Diagnostics;
using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Training Dmg")]
public class TrainingDummy : MonsterScript
{
    private Stopwatch _stopwatch = new();
    private long _damage;

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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Lv:{level}, Ac:{ac}, Def:{defEle}");
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        if (source is not Aisling) return;

        if (dmg + _damage >= long.MaxValue)
        {
            _damage = long.MaxValue;
        }
        else
        {
            _damage += dmg;
        }

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

        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }

        if (_stopwatch.Elapsed.TotalMilliseconds < 1000) return;
        _stopwatch.Restart();
        if (_damage <= 0) return;
        Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Dummy: {{=q{_damage:N0} {{=areceived\n"));
        _damage = 0;
    }
}

[Script("Training Skills")]
public class TrainingDummy2 : MonsterScript
{
    private DmgTable _incoming;

    public TrainingDummy2(Monster monster, Area map) : base(monster, map)
    {
        Monster.BonusMr = 0;
        Monster.MonsterBank = new List<Item>();
    }

    public override void OnClick(WorldClient client)
    {
        var level = Monster.Template.Level.ToString();
        var ac = Monster.SealedAc.ToString();
        var defEle = ElementManager.ElementValue(Monster.DefenseElement);

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Lv:{level}, Ac:{ac}, Def:{defEle}");
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