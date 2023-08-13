using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Bite")]
public class Bite : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod;

    public Bite(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Bite'n Shake")]
public class BiteAndShake : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod;

    public BiteAndShake(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var debuff = new debuff_beagsuain();

        if (!_target.HasDebuff(debuff.Name))
            debuff.OnApplied(_target, debuff);

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
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
public class CorrosiveTouch : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public CorrosiveTouch(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var debuff = new debuff_rend();

        if (!_target.HasDebuff(debuff.Name) || !_target.HasDebuff("Hurricane"))
            debuff.OnApplied(_target, debuff);
        if (_target is Aisling targetPlayer)
            targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
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
public class Stomp : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Stomp(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
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
public class HeadButt : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public HeadButt(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
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
public class Claw : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Claw(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
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
public class MuleKick : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public MuleKick(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(_skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, _skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 6;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}