using System.Numerics;
using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
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
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
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
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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

[Script("Tail Slap")]
public class TailSlap(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = sprite.MonsterGetInFrontToSide();

        if (enemy.Count == 0)
        {
            OnFailed(sprite);
            return;
        }

        foreach (var i in enemy.Where(i => i.Attackable))
        {
            _target = i;
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, sprite, Skill, dmgCalc, _crit);
        }

        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 6 + damageMonster.Dex * 12;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Roll Over")]
public class RollOver(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var debuff = new DebuffBleeding();

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
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Con * 6 + damageMonster.Dex * 8;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Swallow Whole")]
public class SwallowWhole(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var debuff = new DebuffBleeding();

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
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Int * 6 + damageMonster.Wis * 8;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Howl'n Call")]
public class HowlAndCall(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Monster casterMonster) return;
        if (casterMonster.Target is null) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (casterMonster.Target is not { CurrentHp: > 1 })
        {
            if (casterMonster.Target is not Aisling aisling) return;
            casterMonster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            casterMonster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;

        var monstersNearby = casterMonster.MonstersOnMap();

        foreach (var monster in monstersNearby)
        {
            if (monster.WithinRangeOf(casterMonster)) continue;

            var readyTime = DateTime.UtcNow;
            monster.Pos = new Vector2(casterMonster.Pos.X, casterMonster.Pos.Y);

            foreach (var player in casterMonster.AislingsNearby())
            {
                player.Client.SendCreatureWalk(monster.Serial, new Point(casterMonster.X, casterMonster.Y), (Direction)casterMonster.Direction);
            }

            monster.LastMovementChanged = readyTime;
            monster.LastPosition = new Position(casterMonster.X, casterMonster.Y);
            break;
        }

        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }
}

[Script("Death From Above")]
public class DeathFromAbove(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 15 + damageMonster.Str * 3;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Pounce")]
public class Pounce(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = sprite.MonsterGetInFront(6).FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(sprite, Skill, action);
            OnFailed(sprite);
            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 9 + damageMonster.Str * 9 + damageMonster.Con * 9;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tentacle")]
public class Tentacle(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
            _skillMethod.FailedAttempt(sprite, Skill, action);
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
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
        _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 4 + damageMonster.Dex * 4 + damageMonster.Con * 2;
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
        if (!Skill.CanUse()) return;

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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

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
                _skillMethod.FailedAttempt(sprite, Skill, action);
                OnFailed(sprite);
                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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

[Script("Fire Wheel")]
public class FireWheel(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemyList = sprite.MonsterGetFiveByFourRectInFront();
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
                _skillMethod.FailedAttempt(sprite, Skill, action);
                OnFailed(sprite);
                continue;
            }

            if (_target is not Aisling aisling) continue;
            var dmgCalc = DamageCalc(sprite);
            var fireCalc = FireDamageCalc(sprite);
            aisling.ApplyElementalSkillDamage(sprite, fireCalc, ElementManager.Element.Fire, Skill);
            _skillMethod.OnSuccessWithoutAction(aisling, sprite, Skill, dmgCalc, _crit);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(223, aisling.Position));
        }

        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    private long FireDamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Int * 72 + damageMonster.Wis * 38;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 95 + damageMonster.Dex * 86;
        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Megaflare")]
public class Megaflare(Skill skill) : SkillScript(skill)
{
    private readonly List<Sprite> _targets = new();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        var nearby = sprite.GetObjects<Aisling>(sprite.Map, i => i != null && i.WithinRangeOf(sprite, 6)).ToArray();

        foreach (var player in nearby)
        {
            if (player == null) continue;
            var rand = Generator.RandomNumPercentGen();
            if (rand >= .60) continue;
            _targets.Add(player);
        }

        if (_targets.Count == 0)
        {
            OnFailed(sprite);
            return;
        }

        foreach (var enemy in _targets.Where(enemy => enemy != null && enemy.Serial != sprite.Serial && enemy.Attackable))
        {
            if (enemy is not Aisling aisling) continue;
            var dmgCalc = DamageCalc(sprite);
            aisling.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, Skill);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(217, aisling.Position));
        }

        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    private long DamageCalc(Sprite sprite)
    {
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 65 + damageMonster.Int * 90;
        var critCheck = _skillMethod.OnCrit(dmg);
        return critCheck.Item2;
    }
}

[Script("Lava Armor")]
public class LavaArmor(Skill skill) : SkillScript(skill)
{
    private readonly Buff _buff1 = new buff_skill_reflect();
    private readonly Buff _buff2 = new buff_spell_reflect();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        _skillMethod.ApplyPhysicalBuff(sprite, _buff1);
        _skillMethod.ApplyPhysicalBuff(sprite, _buff2);
    }
}