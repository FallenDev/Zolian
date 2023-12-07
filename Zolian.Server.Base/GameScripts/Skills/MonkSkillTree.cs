using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Ambush")]
public class Ambush(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private IList<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Ambush";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var targetPos = aisling.GetFromAllSidesEmpty(aisling, _target);

        if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _skillMethod.Step(aisling, targetPos.X, targetPos.Y);
        aisling.Facing(_target.X, _target.Y, out var direction);
        aisling.Direction = (byte)direction;
        aisling.Turn();
        _skillMethod.OnSuccess(_target, aisling, skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is Aisling aisling)
            Target(aisling);
    }


    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = skill.Level * 2;
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

    private void Target(Aisling sprite)
    {
        var client = sprite.Client;

        _enemyList = client.Aisling.DamageableGetInFront(3);
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(sprite);
        }
        else
        {
            _success = _skillMethod.OnUse(sprite, skill);

            if (_success)
            {
                OnSuccess(sprite);
            }
            else
            {
                OnFailed(sprite);
            }
        }
    }
}

[Script("Wolf Fang Fist")]
public class WolfFangFist(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wolf Fang Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.HasDebuff("Beag Suain"))
            _target.RemoveDebuff("Beag Suain");

        var debuff = new DebuffFrozen();
        {
            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, skill);
        }

        _skillMethod.OnSuccess(_target, aisling, skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                        var debuff = new DebuffFrozen();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Knife Hand Strike")]
public class KnifeHandStrike(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Knife Hand Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, aisling, skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Dex * 7 + client.Aisling.Str * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Palm Heel Strike")]
public class PalmHeelStrike(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Palm Heel Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront();
        var enemy2 = aisling.DamageableGetBehind();
        enemy.AddRange(enemy2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Con * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Hammer Twist")]
public class HammerTwist(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Hammer Twist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Dex * 1;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Dex * 1;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Cross Body Punch")]
public class CrossBodyPunch(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Cross Body Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(3);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront(3).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 3 + client.Aisling.Dex * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Hurricane Kick")]
public class HurricaneKick(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Hurricane Kick";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = _skillMethod.GetInCone(aisling);

        if (enemy.Length == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            var debuff = new DebuffHurricane();

            if (!_target.HasDebuff(debuff.Name))
                aisling.Client.EnqueueDebuffAppliedEvent(_target, debuff, TimeSpan.FromSeconds(debuff.Length));

            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Vitality);

            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var debuff = new DebuffHurricane();

            if (_target is Aisling targetPlayer)
            {
                if (!_target.HasDebuff(debuff.Name) || !_target.HasDebuff("Rend"))
                {
                    targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff, TimeSpan.FromSeconds(debuff.Length));
                    targetPlayer.Client.SendAttributes(StatUpdateType.Vitality);
                }
            }
            else
            {
                if (!_target.HasDebuff(debuff.Name) || !_target.HasDebuff("Rend"))
                    debuff.OnApplied(_target, debuff);
            }

            var dmg = (int)(sprite.MaximumHp * 1.2);
            sprite.CurrentHp = (int)(sprite.CurrentHp * 0.8);
            _skillMethod.OnSuccess(_target, sprite, skill, dmg, false, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Con * 6;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Con * 6;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Kelberoth Strike")]
public class Kelberoth_Strike(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Kelberoth Strike";

        var criticalHp = (int)(aisling.MaximumHp * .33);
        var kelbHp = (int)(aisling.CurrentHp * .66);

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var dmg = (int)(criticalHp * 2.5);
        aisling.CurrentHp = kelbHp >= aisling.CurrentHp ? 1 : kelbHp;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ahhhhh!");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
        _skillMethod.OnSuccess(_target, aisling, skill, dmg, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmg = (int)(sprite.CurrentHp * 2.5);
            _skillMethod.OnSuccess(_target, sprite, skill, dmg, false, action);
        }
    }
}

[Script("Krane Kick")]
public class Krane_Kick(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Krane Kick";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Kick,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(2);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront(2).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Con * 4 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 4;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Claw Fist")]
public class Claw_Fist(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to enhance assails.");
        damageDealingAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, damageDealingAisling.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Claw Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 70,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.HasBuff("Claw Fist"))
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var buff = new buff_clawfist();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.OnSuccess(aisling, aisling, skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, skill);

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

[Script("Ember Strike")]
public class EmberStrike(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Ember Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var spellMethod = new GlobalSpellMethods();
        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            dmgCalc += (int)spellMethod.WeaponDamageElementalProc(aisling, 1);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, skill);
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(17, null, _target.Serial));
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, 0, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + skill.Level;
            dmg = client.Aisling.Con * 9;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 9;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Pummel")]
public class Pummel(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Pummel";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = sprite.DamageableGetInFront();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutActionAnimation(_target, aisling, skill, dmgCalc, _crit);
            _skillMethod.OnSuccessWithoutActionAnimation(_target, aisling, skill, dmgCalc, _crit);
            _skillMethod.OnSuccessWithoutActionAnimation(_target, aisling, skill, dmgCalc, _crit);
            _skillMethod.OnSuccessWithoutActionAnimation(_target, aisling, skill, dmgCalc, _crit);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = Skill.Reflect(i, sprite, skill);

                var dmgCalc = DamageCalc(sprite);

                _target.ApplyDamage(sprite, dmgCalc, skill);
                _target.ApplyDamage(sprite, dmgCalc, skill);
                _target.ApplyDamage(sprite, dmgCalc, skill);
                _target.ApplyDamage(sprite, dmgCalc, skill);
                _target.ApplyDamage(sprite, dmgCalc, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 8;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 8;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Thump")]
public class Thump(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Thump";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.HasDebuff("Beag Suain"))
            _target.RemoveDebuff("Beag Suain");

        var debuff = new DebuffAdvFrozen();
        {
            if (_target.HasDebuff("Frozen"))
                _target.RemoveDebuff("Frozen");

            if (_target.HasDebuff(debuff.Name))
                _target.RemoveDebuff(debuff.Name);

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, skill);
        }

        _skillMethod.OnSuccess(_target, aisling, skill, 2500, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                        var debuff = new DebuffAdvFrozen();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Eye Gouge")]
public class EyeGouge(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to blind");
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Eye Gouge";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var debuff = new DebuffBlind();
        {
            if (_target.HasDebuff("Blind"))
                _target.RemoveDebuff("Blind");

            _skillMethod.ApplyPhysicalDebuff(aisling.Client, debuff, _target, skill);
        }

        _skillMethod.OnSuccess(_target, aisling, skill, 1800, true, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                        var debuff = new DebuffBlind();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                _skillMethod.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Calming Mist")]
public class Calming_Mist(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed");
        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, sprite.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Calming Mist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Peace,
            Sound = null,
            SourceId = sprite.Serial
        };

        aisling.ThreatMeter /= 4;
        _skillMethod.Train(client, skill);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, aisling.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, skill);

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

[Script("Healing Palms")]
public class HealingPalms(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Healing Palms";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = sprite.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            if (_target.CurrentHp <= 1) continue;
            var dmgCalc = DamageCalc(sprite);
            _target.CurrentHp += (int)dmgCalc;

            if (_target.CurrentHp > _target.MaximumHp)
                _target.CurrentHp = _target.MaximumHp;

            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendHealthBar(_target, skill.Template.Sound));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial));

            if (_target is Aisling targetPlayer)
                targetPlayer.Client.SendAttributes(StatUpdateType.Vitality);
        }
        
        _skillMethod.Train(aisling.Client, skill);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var manaReq = aisling.MaximumHp * .10;

        if (aisling.CurrentMp >= manaReq)
        {
            aisling.CurrentMp -= (int)manaReq;
            OnSuccess(aisling);
            return;
        }
        
        OnFailed(aisling);
    }

    private long DamageCalc(Sprite sprite)
    {
        long dmg = 0;
        if (sprite is not Aisling damageDealingAisling) return dmg;
        var client = damageDealingAisling.Client;
        var imp = 50 + skill.Level;
        dmg = client.Aisling.Wis * 6 + client.Aisling.Con * 6;
        dmg += dmg * imp / 100;
        return dmg;
    }
}

[Script("Ninth Gate")]
public class NinthGate(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Ninth Gate";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var buff = new buff_ninthGate();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.OnSuccess(aisling, aisling, skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Drunken Fist")]
public class DrunkenFist(Skill skill) : SkillScript(skill)
{
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Drunken Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp2,
            Sound = null,
            SourceId = sprite.Serial
        };

        var buff = new buff_drunkenFist();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.OnSuccess(aisling, aisling, skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}