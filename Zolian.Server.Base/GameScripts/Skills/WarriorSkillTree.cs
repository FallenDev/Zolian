using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Wind Slice")]
public class Wind_Slice : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Wind_Slice(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Wind Slice has missed the target.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Wind Slice";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront(4);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Dual Slice")]
public class Dual_Slice : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Dual_Slice(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Dual Slice has missed the target.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Dual Slice";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Blitz")]
public class Blitz : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Blitz(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "*Stumbled*");
        if (_target == null) return;
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Blitz";

        var dmgCalc = DamageCalc(damageDealingSprite);

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        if (_target == null)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        if (_target.Serial == damageDealingSprite.Serial)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        var targetPos = damageDealingSprite.GetFromAllSidesEmpty(damageDealingSprite, _target);

        if (targetPos == null || targetPos == _target.Position)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        _skillMethod.Step(damageDealingSprite, targetPos.X, targetPos.Y);
        _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

        damageDealingSprite.Facing(_target.X, _target.Y, out var direction);

        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, new Vector2(targetPos.X, targetPos.Y)));

        damageDealingSprite.Direction = (byte)direction;
        damageDealingSprite.Turn();
        _skillMethod.Train(client, _skill);

        if (_crit)
        {
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        Target(aisling);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = _skill.Level * 2;
            dmg = (int)(client.Aisling.Str * 1 + client.Aisling.Dex * 1.3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 1 + damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        _enemyList = client.Aisling.DamageableGetInFront(3);
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(damageDealingSprite);
        }
        else
        {
            _success = _skillMethod.OnUse(damageDealingSprite, _skill);

            if (_success)
            {
                OnSuccess(damageDealingSprite);
            }
            else
            {
                OnFailed(damageDealingSprite);
            }
        }
    }
}

[Script("Aid")]
public class Aid : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private readonly GlobalSkillMethods _skillMethod;

    public Aid(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Aid failed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Aid";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, sprite, _skill);
            if (_target == null) continue;

            switch (_target)
            {
                case Aisling reviveAisling:
                    {
                        if (reviveAisling.Skulled)
                        {
                            reviveAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                            if (debuff != null)
                            {
                                debuff.Cancelled = true;
                                debuff.OnEnded(reviveAisling, debuff);
                                reviveAisling.Client.Revive();
                            }
                        }

                        if (reviveAisling.HasDebuff("Beag Suain"))
                            reviveAisling.Debuffs.TryRemove("Beag Suain", out _);

                        _target.ApplyDamage(aisling, 0, _skill);
                        reviveAisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
                        break;
                    }
            }
        }

        _skillMethod.Train(client, _skill);
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!_skill.CanUse()) return;
        aisling.Client.Send(new ServerFormat3F(1, _skill.Slot, _skill.Template.Cooldown));

        var success = Generator.RandNumGen100();

        if (success < 3)
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Lullaby Strike")]
public class Lullaby_Strike : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Lullaby_Strike(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed to incapacitate.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Lullaby Strike";

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy == null)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            var target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (target == null) continue;

            if (target.HasDebuff("Beag Suain"))
                target.RemoveDebuff("Beag Suain");

            var debuff = new debuff_frozen();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                _skillMethod.ApplyPhysicalDebuff(client, debuff, target, _skill);
            }

            _skillMethod.Train(client, _skill);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var target = sprite.Target;

            switch (target)
            {
                case null:
                    return;
                case Aisling damageDealingTarget:
                    {
                        var debuff = new debuff_frozen();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, _skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Desolate")]
public class Desolate : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Desolate(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Desolate has missed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Desolate";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.GetHorizontalInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 6;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Crasher")]
public class Crasher : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Crasher(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "I missed...");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Crasher";

        var criticalHp = (int)(client.Aisling.MaximumHp * .95);

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        _target = enemy.First();
        _target = Skill.Reflect(_target, damageDealingSprite, _skill);
        var dmg = (int)(damageDealingSprite.MaximumHp * 1.5);
        _target.ApplyDamage(damageDealingSprite, dmg, _skill);
        _skillMethod.Train(client, _skill);

        if (client.Aisling.CurrentHp > criticalHp)
        {
            client.Aisling.CurrentHp -= criticalHp;
        }

        client.SendMessage(0x02, "I feel drained...");
        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
        damageDealingSprite.Client.Send(new ServerFormat08(damageDealingSprite, StatusFlags.StructB));
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var target = sprite.Target;
            if (target == null) return;
            target = Skill.Reflect(target, sprite, _skill);

            sprite.Show(Scope.NearbyAislings,
                new ServerFormat29((uint)target.Serial, (uint)sprite.Serial,
                    _skill.Template.TargetAnimation, 0, 100));

            var dmg = sprite.MaximumHp * 30 / 100;
            target.ApplyDamage(sprite, dmg, _skill);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x82,
                Speed = 20
            };

            sprite.CurrentHp = (int)(sprite.CurrentHp * .50);
            sprite.Show(Scope.NearbyAislings, action);
        }
    }
}

[Script("Sever")]
public class Sever : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Sever(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Sever has missed the target.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Sever";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = (int)(client.Aisling.Str * 5 + client.Aisling.Dex * 1.5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Rush")]
public class Rush : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Rush(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "*Stumbled*");
        if (_target == null) return;
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Rush";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        foreach (var i in _enemyList.Where(i => i.Attackable))
        {
            if (i != _target) continue;
            var dmgCalc = DamageCalc(damageDealingSprite);
            var position = _target.Position;
            var mapCheck = damageDealingSprite.Map.ID;
            var wallPosition = damageDealingSprite.GetPendingChargePosition(3, damageDealingSprite);
            var targetPos = _skillMethod.DistanceTo(damageDealingSprite.Position, position);
            var wallPos = _skillMethod.DistanceTo(damageDealingSprite.Position, wallPosition);

            if (mapCheck != damageDealingSprite.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (damageDealingSprite.Direction)
                {
                    case 0:
                        position.Y++;
                        break;
                    case 1:
                        position.X--;
                        break;
                    case 2:
                        position.Y--;
                        break;
                    case 3:
                        position.X++;
                        break;
                }

                if (damageDealingSprite.Position != position)
                {
                    _skillMethod.Step(damageDealingSprite, position.X, position.Y);
                }

                _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

                damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

                _skillMethod.Train(client, _skill);

                if (!_crit) continue;
                client.Aisling.Animate(387);
                _crit = false;
            }
            else
            {
                _skillMethod.Step(damageDealingSprite, wallPosition.X, wallPosition.Y);

                var stunned = new debuff_beagsuain();
                stunned.OnApplied(damageDealingSprite, stunned);

                damageDealingSprite.Animate(208);
            }
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (!client.Aisling.CanAttack)
        {
            OnFailed(aisling);
            return;
        }

        Target(aisling);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = _skill.Level * 2;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        _enemyList = client.Aisling.DamageableGetInFront(3);
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, _skill);

        if (_target == null)
        {
            var mapCheck = damageDealingSprite.Map.ID;
            var wallPosition = damageDealingSprite.GetPendingChargePosition(3, damageDealingSprite);
            var wallPos = _skillMethod.DistanceTo(damageDealingSprite.Position, wallPosition);

            if (mapCheck != damageDealingSprite.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(damageDealingSprite);

            if (damageDealingSprite.Position != wallPosition)
            {
                _skillMethod.Step(damageDealingSprite, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 2)
            {
                var stunned = new debuff_beagsuain();
                stunned.OnApplied(damageDealingSprite, stunned);
                damageDealingSprite.Animate(208);
            }

            damageDealingSprite.UsedSkill(_skill);
            client.Aisling.Show(Scope.NearbyAislings, action);
        }
        else
        {
            _success = _skillMethod.OnUse(damageDealingSprite, _skill);

            if (_success)
            {
                OnSuccess(damageDealingSprite);
            }
            else
            {
                OnFailed(damageDealingSprite);
            }
        }
    }
}

[Script("Rescue")]
public class Rescue : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private readonly GlobalSkillMethods _skillMethod;

    public Rescue(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Rescue failed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rescue";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x01,
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, sprite, _skill);
            if (_target == null) continue;

            switch (_target)
            {
                case Aisling reviveAisling:
                    {
                        if (reviveAisling.Skulled)
                        {
                            reviveAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                            if (debuff != null)
                            {
                                debuff.Cancelled = true;
                                debuff.OnEnded(reviveAisling, debuff);
                                reviveAisling.Client.Revive();
                            }
                        }

                        if (reviveAisling.HasDebuff("Beag Suain"))
                            reviveAisling.Debuffs.TryRemove("Beag Suain", out _);

                        _target.ApplyDamage(aisling, 0, _skill);
                        reviveAisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
                        break;
                    }
                case Monster _:
                    _target.ApplyDamage(aisling, 3 * aisling.Int, _skill);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
                    break;
            }
        }
        _skillMethod.Train(client, _skill);

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!_skill.CanUse()) return;
        aisling.Client.Send(new ServerFormat3F(1, _skill.Slot, _skill.Template.Cooldown));

        var success = Generator.RandNumGen100();

        if (success < 3)
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Wind Blade")]
public class Wind_Blade : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Wind_Blade(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Wind Blade has missed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Wind Blade";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront(4);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 40 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Dex * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Beag Suain")]
public class Beag_Suain : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Beag_Suain(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed to incapacitate.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Beag Suain";

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy == null)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            var target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (target == null) continue;

            var debuff = new debuff_beagsuain();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                _skillMethod.ApplyPhysicalDebuff(client, debuff, target, _skill);
            }

            _skillMethod.Train(client, _skill);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var target = sprite.Target;

            switch (target)
            {
                case null:
                    return;
                case Aisling damageDealingTarget:
                    {
                        var debuff = new debuff_beagsuain();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, _skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Vampiric Slash")]
public class Vampiric_Slash : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Vampiric_Slash(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Vampiric Slash missed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Vampiric Slash";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            damageDealingSprite.CurrentHp += dmgCalc;
            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            damageDealingSprite.Client.Send(new ServerFormat08(damageDealingSprite, StatusFlags.StructB));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);
                var healthAbsorb = dmgCalc * 25;
                sprite.CurrentHp += healthAbsorb;
                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)sprite.Serial, (uint)_target.Serial,
                                _skill.Template.TargetAnimation, 0, 100));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 60 + _skill.Level;
            dmg = client.Aisling.Int * 5 + client.Aisling.Wis * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Charge")]
public class Charge : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Charge(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "*Stumbled*");
        if (_target == null) return;
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Charge";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        foreach (var i in _enemyList.Where(i => i.Attackable))
        {
            if (i != _target) continue;
            var dmgCalc = DamageCalc(damageDealingSprite);
            var position = _target.Position;
            var mapCheck = damageDealingSprite.Map.ID;
            var wallPosition = damageDealingSprite.GetPendingChargePosition(7, damageDealingSprite);
            var targetPos = _skillMethod.DistanceTo(damageDealingSprite.Position, position);
            var wallPos = _skillMethod.DistanceTo(damageDealingSprite.Position, wallPosition);

            if (mapCheck != damageDealingSprite.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (damageDealingSprite.Direction)
                {
                    case 0:
                        position.Y++;
                        break;
                    case 1:
                        position.X--;
                        break;
                    case 2:
                        position.Y--;
                        break;
                    case 3:
                        position.X++;
                        break;
                }

                if (damageDealingSprite.Position != position)
                {
                    _skillMethod.Step(damageDealingSprite, position.X, position.Y);
                }

                _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

                damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

                _skillMethod.Train(client, _skill);

                if (!_crit) continue;
                client.Aisling.Animate(387);
                _crit = false;
            }
            else
            {
                _skillMethod.Step(damageDealingSprite, wallPosition.X, wallPosition.Y);

                var stunned = new debuff_beagsuain();
                stunned.OnApplied(damageDealingSprite, stunned);

                damageDealingSprite.Animate(208);
            }
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (!client.Aisling.CanAttack)
        {
            OnFailed(aisling);
            return;
        }

        Target(aisling);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = _skill.Level * 2;
            dmg = client.Aisling.Str * 5 + client.Aisling.Con * 4;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5 + damageMonster.Con * 4;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        _enemyList = client.Aisling.DamageableGetInFront(7);
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, _skill);

        if (_target == null)
        {
            var mapCheck = damageDealingSprite.Map.ID;
            var wallPosition = damageDealingSprite.GetPendingChargePosition(7, damageDealingSprite);
            var wallPos = _skillMethod.DistanceTo(damageDealingSprite.Position, wallPosition);

            if (mapCheck != damageDealingSprite.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(damageDealingSprite);

            if (damageDealingSprite.Position != wallPosition)
            {
                _skillMethod.Step(damageDealingSprite, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 6)
            {
                var stunned = new debuff_beagsuain();
                stunned.OnApplied(damageDealingSprite, stunned);
                damageDealingSprite.Animate(208);
            }

            damageDealingSprite.UsedSkill(_skill);
            client.Aisling.Show(Scope.NearbyAislings, action);
        }
        else
        {
            _success = _skillMethod.OnUse(damageDealingSprite, _skill);

            if (_success)
            {
                OnSuccess(damageDealingSprite);
            }
            else
            {
                OnFailed(damageDealingSprite);
            }
        }
    }
}

[Script("Titan's Cleave")]
public class Titans_Cleave : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Titans_Cleave(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "That's Impossible!");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Titan's Cleave";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded ? 0x81 : 0x01
                : 0x01),
            Speed = 20
        };

        var enemy = _skillMethod.GetInCone(damageDealingSprite);

        if (enemy.Length == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            var debuff = new debuff_rend();
            if (!_target.HasDebuff(debuff.Name)) debuff.OnApplied(_target, debuff);
            damageDealingSprite.Client.Send(new ServerFormat08(damageDealingSprite, StatusFlags.StructD));

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                var debuff = new debuff_rend();
                if (!_target.HasDebuff(debuff.Name)) debuff.OnApplied(_target, debuff);
                if (_target is Aisling targetAisling)
                    targetAisling.Client.Send(new ServerFormat08(targetAisling, StatusFlags.StructD));

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += (int)(damageMonster.Con * 1.2);
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Retribution")]
public class Retribution : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Retribution(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        damageDealingAisling.ActionUsed = "Retribution";

        client.SendMessage(0x02, "That's Impossible!");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = (byte)(client.Aisling.Path == Class.Berserker
                ? 0x8C : 0x01),
            Speed = 30
        };

        var enemy = _skillMethod.GetInCone(damageDealingSprite).ToList();
        var enemyTwo = damageDealingSprite.DamageableGetBehind();
        enemy.Add(enemyTwo.FirstOrDefault());

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var i in enemy.Where(i => i is { Attackable: true }))
        {
            _target = Skill.Reflect(i, damageDealingSprite, _skill);
            if (_target == null) continue;

            var dmgCalc = DamageCalc(damageDealingSprite);

            var debuff = new debuff_rend();
            if (!_target.HasDebuff(debuff.Name)) debuff.OnApplied(_target, debuff);
            damageDealingSprite.Client.Send(new ServerFormat08(damageDealingSprite, StatusFlags.StructD));

            _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

            damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));
            _skillMethod.Train(client, _skill);

            if (!_crit) continue;
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.MonsterGetInFront();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                var debuff = new debuff_rend();
                if (!_target.HasDebuff(debuff.Name)) debuff.OnApplied(_target, debuff);
                if (_target is Aisling targetAisling)
                    targetAisling.Client.Send(new ServerFormat08(targetAisling, StatusFlags.StructD));

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings,
                            new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 9 + client.Aisling.Dex * 3 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 9;
            dmg += damageMonster.Con * 3 + damageMonster.Dex * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Beag Suain Ia Gar")]
public class Beag_Suain_Ia_Gar : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Beag_Suain_Ia_Gar(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed to incapacitate.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Beag Suain Ia Gar";

        var list = sprite.MonstersNearby().ToList();

        if (list.Count == 0)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        foreach (var target in list.Where(i => i.Attackable))
        {
            var debuff = new debuff_beagsuaingar();
            {
                if (target.HasDebuff(debuff.Name))
                    target.RemoveDebuff(debuff.Name);

                _skillMethod.ApplyPhysicalDebuff(client, debuff, target, _skill);
            }
        }

        _skillMethod.Train(client, _skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var target = sprite.Target;

            switch (target)
            {
                case null:
                    return;
                case Aisling damageDealingTarget:
                    {
                        var debuff = new debuff_beagsuaingar();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, _skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Sneak Attack")]
public class Sneak_Attack : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Sneak_Attack(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "*Stumbled*");
        if (_target == null) return;
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Sneak Attack";

        var dmgCalc = DamageCalc(damageDealingSprite);

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x82,
            Speed = 20
        };

        if (_target == null)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        if (_target.Serial == damageDealingSprite.Serial)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        var targetPos = damageDealingSprite.GetFromAllSidesEmpty(damageDealingSprite, _target);

        if (targetPos == null || targetPos == _target.Position)
        {
            OnFailed(damageDealingSprite);
            return;
        }

        _skillMethod.Step(damageDealingSprite, targetPos.X, targetPos.Y);
        _target.ApplyDamage(damageDealingSprite, dmgCalc, _skill);

        damageDealingSprite.Facing(_target.X, _target.Y, out var direction);

        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, new Vector2(targetPos.X, targetPos.Y)));

        damageDealingSprite.Direction = (byte)direction;
        damageDealingSprite.Turn();
        _skillMethod.Train(client, _skill);

        if (_crit)
        {
            client.Aisling.Animate(387);
            _crit = false;
        }

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is not Aisling aisling) return;

        Target(aisling);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = _skill.Level * 2;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 5;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        _enemyList = client.Aisling.DamageableGetInFront();
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(damageDealingSprite);
        }
        else
        {
            _success = _skillMethod.OnUse(damageDealingSprite, _skill);

            if (_success)
            {
                OnSuccess(damageDealingSprite);
            }
            else
            {
                OnFailed(damageDealingSprite);
            }
        }
    }
}

[Script("Raise Threat")]
public class Raise_Threat : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Raise_Threat(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Raise Threat";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x06,
            Speed = 70
        };

        damageDealingSprite.ThreatMeter += 250000;

        var enemies = damageDealingSprite.MonstersNearby();
        foreach (var monster in enemies.Where(e => e is { IsAlive: true }))
        {
            monster.Target = damageDealingSprite;
        }

        _skillMethod.Train(client, _skill);

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, _skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }
}