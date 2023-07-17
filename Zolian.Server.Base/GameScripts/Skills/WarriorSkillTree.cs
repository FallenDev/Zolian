using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wind Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(4);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial, 170));

                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Dual Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront(3).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Blitz";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var targetPos = aisling.GetFromAllSidesEmpty(aisling, _target);

        if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _skillMethod.Step(aisling, targetPos.X, targetPos.Y);
        aisling.Facing(_target.X, _target.Y, out var direction);
        aisling.Direction = (byte)direction;
        aisling.Turn();
        _skillMethod.OnSuccess(_target, aisling, _skill, dmgCalc, _crit, action);
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
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        _enemyList = client.Aisling.DamageableGetInFront(3);
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(aisling);
        }
        else
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Aid";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
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
                        reviveAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial));
                        break;
                    }
            }
        }

        _skillMethod.Train(client, _skill);
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!_skill.CanUse()) return;
        aisling.Client.SendCooldown(true, _skill.Slot, _skill.Template.Cooldown);

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
    private Sprite _target;
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Lullaby Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.HasDebuff("Beag Suain"))
            _target.RemoveDebuff("Beag Suain");

        var debuff = new debuff_frozen();
        {
            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, _skill);
        }

        _skillMethod.OnSuccess(_target, aisling, _skill, 0, false, action);
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Desolate";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = client.Aisling.GetHorizontalInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy == null) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial, 170));

                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Crasher";

        var criticalHp = (int)(aisling.MaximumHp * .95);

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.JumpAttack,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var dmg = (int)(aisling.MaximumHp * 1.5);

        if (aisling.CurrentHp > criticalHp)
        {
            aisling.CurrentHp -= criticalHp;
        }

        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel drained...");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
        _skillMethod.OnSuccess(_target, aisling, _skill, dmg, false, action);
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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var dmg = (int)(sprite.MaximumHp * 1.2);
            sprite.CurrentHp = (int)(sprite.CurrentHp * 0.8);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmg, false, action);
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Sever";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial, 170));

                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        if (_target == null) return;
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rush";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Jump,
            Sound = null,
            SourceId = sprite.Serial
        };

        foreach (var i in _enemyList.Where(i => i.Attackable))
        {
            if (i != _target) continue;
            var dmgCalc = DamageCalc(aisling);
            var position = _target.Position;
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(3, aisling);
            var targetPos = _skillMethod.DistanceTo(aisling.Position, position);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (aisling.Direction)
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

                if (aisling.Position != position)
                {
                    _skillMethod.Step(aisling, position.X, position.Y);
                }

                _target.ApplyDamage(aisling, dmgCalc, _skill);
                _skillMethod.Train(client, _skill);
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
            }
            else
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);

                var stunned = new debuff_beagsuain();
                stunned.OnApplied(aisling, stunned);

                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, aisling.Serial));
            }
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
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
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        _enemyList = client.Aisling.DamageableGetInFront(3);
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, _skill);

        if (_target == null)
        {
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(3, aisling);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(aisling);

            if (aisling.Position != wallPosition)
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 2)
            {
                var stunned = new debuff_beagsuain();
                stunned.OnApplied(aisling, stunned);
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, aisling.Serial));
            }

            aisling.UsedSkill(_skill);
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        else
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rescue";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = client.Aisling.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
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
                        reviveAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial));
                        break;
                    }
                case Monster _:
                    _target.ApplyDamage(aisling, 3 * aisling.Int, _skill);
                    sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial));
                    break;
            }
        }
        _skillMethod.Train(client, _skill);

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!_skill.CanUse()) return;
        aisling.Client.SendCooldown(true, _skill.Slot, _skill.Template.Cooldown);

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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Wind Blade";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial, 170));

                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
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
    private Sprite _target;
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Beag Suain";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var debuff = new debuff_frozen();
        {
            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, _skill);
        }
        
        _skillMethod.OnSuccess(_target, aisling, _skill, 0, false, action);
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Vampiric Slash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            aisling.CurrentHp += dmgCalc;
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
            var enemy = sprite.MonsterGetInFront(2);

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, _skill);

                var dmgCalc = DamageCalc(sprite);
                var healthAbsorb = dmgCalc * 5;
                sprite.CurrentHp += healthAbsorb;
                _target.ApplyDamage(sprite, dmgCalc, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial, 170));

                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        if (_target == null) return;
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Charge";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Jump,
            Sound = null,
            SourceId = sprite.Serial
        };

        foreach (var i in _enemyList.Where(i => i.Attackable))
        {
            if (i != _target) continue;
            var dmgCalc = DamageCalc(aisling);
            var position = _target.Position;
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(7, aisling);
            var targetPos = _skillMethod.DistanceTo(aisling.Position, position);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (aisling.Direction)
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

                if (aisling.Position != position)
                {
                    _skillMethod.Step(aisling, position.X, position.Y);
                }

                _target.ApplyDamage(aisling, dmgCalc, _skill);
                _skillMethod.Train(client, _skill);
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.TargetAnimation, _target.Serial));

                if (!_crit) return;
                sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, sprite.Serial));
            }
            else
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);

                var stunned = new debuff_beagsuain();
                stunned.OnApplied(aisling, stunned);

                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, aisling.Serial));
            }
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
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
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        _enemyList = client.Aisling.DamageableGetInFront(7);
        _target = _enemyList.FirstOrDefault();
        _target = Skill.Reflect(_target, sprite, _skill);

        if (_target == null)
        {
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(7, aisling);
            var wallPos = _skillMethod.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(aisling);

            if (aisling.Position != wallPosition)
            {
                _skillMethod.Step(aisling, wallPosition.X, wallPosition.Y);
            }

            if (wallPos <= 6)
            {
                var stunned = new debuff_beagsuain();
                stunned.OnApplied(aisling, stunned);
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, aisling.Serial));
            }

            aisling.UsedSkill(_skill);
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        else
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Titan's Cleave";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = _skillMethod.GetInCone(aisling);

        if (enemy.Length == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(aisling);
            var debuff = new debuff_rend();

            if (!_target.HasDebuff(debuff.Name)) 
                debuff.OnApplied(_target, debuff);
            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var debuff = new debuff_rend();

            if (!_target.HasDebuff(debuff.Name)) 
                debuff.OnApplied(_target, debuff);
            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

            var dmg = sprite.MaximumHp * 1;
            sprite.CurrentHp = (int)(sprite.CurrentHp * 0.3);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmg, false, action);
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
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Retribution";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = _skillMethod.GetInCone(aisling).ToList();
        var enemyTwo = aisling.DamageableGetBehind();
        enemy.Add(enemyTwo.FirstOrDefault());

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => i != null && aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            var debuff = new debuff_rend();
            if (!_target.HasDebuff(debuff.Name)) 
                debuff.OnApplied(_target, debuff);
            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
        }

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Beag Suain Ia Gar";

        var list = sprite.MonstersNearby().ToList();

        if (list.Count == 0)
        {
            OnFailed(aisling);
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        if (_target == null) return;
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Sneak Attack";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Jump,
            Sound = null,
            SourceId = sprite.Serial
        };

        var targetPos = aisling.GetFromAllSidesEmpty(aisling, _target);

        if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _skillMethod.Step(aisling, targetPos.X, targetPos.Y);
        aisling.Facing(_target.X, _target.Y, out var direction);
        aisling.Direction = (byte)direction;
        aisling.Turn();
        _skillMethod.OnSuccess(_target, aisling, _skill, dmgCalc, _crit, action);
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
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        _enemyList = client.Aisling.DamageableGetInFront();
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(aisling);
        }
        else
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

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Raise Threat";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        aisling.ThreatMeter += 250000;

        var enemies = aisling.MonstersNearby();
        foreach (var monster in enemies.Where(e => e is { IsAlive: true }))
        {
            monster.Target = aisling;
        }

        _skillMethod.Train(client, _skill);

        sprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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