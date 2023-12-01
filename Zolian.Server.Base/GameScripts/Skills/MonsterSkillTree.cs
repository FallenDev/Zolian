using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Bite")]
public class Bite(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Gatling")]
public class Gatling(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var nearby = sprite.AislingsNearby();

        foreach (var player in nearby)
        {
            if (player == null) continue;
            var rand = Generator.RandNumGen100();
            if (rand >= 60) continue;
            _target = player;
        }


        if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(sprite, skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 5 + damageMonster.Dex * 8;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Bite'n Shake")]
public class BiteAndShake(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

        var debuff = new DebuffBeagsuain();

        if (_target is Aisling targetPlayer)
        {
            if (!_target.HasDebuff(debuff.Name))
            {
                targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff, TimeSpan.FromSeconds(debuff.Length));
            }
        }
        else
        {
            if (!_target.HasDebuff(debuff.Name))
                debuff.OnApplied(_target, debuff);
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 4 + damageMonster.Dex * 4;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Corrosive Touch")]
public class CorrosiveTouch(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

        var debuff = new DebuffCorrosiveTouch();

        if (_target is Aisling targetPlayer)
        {
            if (!_target.HasDebuff(debuff.Name))
            {
                targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff, TimeSpan.FromSeconds(debuff.Length));
                targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);
            }
        }
        else
        {
            if (!_target.HasDebuff(debuff.Name))
                debuff.OnApplied(_target, debuff);
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 2 + damageMonster.Con * 2;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Stomp")]
public class Stomp(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 3;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Head Butt")]
public class HeadButt(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 2 + damageMonster.Dex * 4;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Claw")]
public class Claw(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 5;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Mule Kick")]
public class MuleKick(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 6;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Omega Avoid")]
public class OmegaAvoid(Skill skill) : SkillScript(skill)
{
    private readonly Buff _buff1 = new buff_MorDion();
    private readonly Buff _buff2 = new buff_PerfectDefense();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        _skillMethod.ApplyPhysicalBuff(sprite, _buff1);
        _skillMethod.ApplyPhysicalBuff(sprite, _buff2);
    }
}

[Script("Omega Slash")]
public class OmegaSlash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemyList = sprite.MonsterGetInFrontToSide();
        if (enemyList.Count == 0)
        {
            OnFailed(sprite);
            return;
        }

        foreach (var enemy in enemyList)
        {
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 12 + damageMonster.Dex * 12;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}