using System.Diagnostics;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Training Dmg")]
public class TrainingDummy : MonsterScript
{
    private readonly Stopwatch _stopwatch = new();
    private long _damage;

    public TrainingDummy(Monster monster, Area map) : base(monster, map)
    {
        Monster.BonusMr = 0;
        Monster.BonusAc = 0;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }

        if (_stopwatch.Elapsed.TotalMilliseconds < 1000) return;
        _stopwatch.Restart();
        if (_damage <= 0) return;
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Dummy: {{=q{_damage:N0} {{=areceived\n"));
        _damage = 0;
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
        if (Monster.CurrentHp < Monster.MaximumHp)
            Monster.CurrentHp = Monster.MaximumHp;

        foreach (var debuff in Monster.Debuffs.Values)
        {
            if (debuff != null)
                Monster.RemoveDebuff(debuff.Name);
        }

        foreach (var debuff in Monster.Buffs.Values)
        {
            if (debuff != null)
                Monster.RemoveBuff(debuff.Name);
        }

        Monster.Skulled = false;
        Monster.BonusAc = 0;
    }
}

[Script("Training Skills")]
public class TrainingDummy2 : MonsterScript
{
    private DmgTable _incoming;

    public TrainingDummy2(Monster monster, Area map) : base(monster, map)
    {
        Monster.BonusAc = 0;
        Monster.BonusMr = 0;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime) { }

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

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}'s {_incoming.What}: {dmgDisplay} DMG.\n"));
        Monster.Facing((int)source.Pos.X, (int)source.Pos.Y, out var direction);

        if (!Monster.Position.IsNextTo(source.Position)) return;
        Monster.Direction = (byte)direction;
        Monster.Turn();
    }

    public override void OnDeath(WorldClient client = null)
    {
        if (Monster.CurrentHp < Monster.MaximumHp)
            Monster.CurrentHp = Monster.MaximumHp;

        foreach (var debuff in Monster.Debuffs.Values)
        {
            if (debuff != null)
                Monster.RemoveDebuff(debuff.Name);
        }

        foreach (var debuff in Monster.Buffs.Values)
        {
            if (debuff != null)
                Monster.RemoveBuff(debuff.Name);
        }

        Monster.Skulled = false;
        Monster.BonusAc = 0;
    }

    private struct DmgTable
    {
        public int Damage { get; set; }
        public string What { get; set; }
    }
}