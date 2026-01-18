using System.Numerics;

using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Bite")]
public class Bite(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 5;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Gatling")]
public class Gatling(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            var nearby = sprite.AislingsNearby().RandomIEnum();

            if (nearby == null || nearby.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(nearby, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 5 + damageMonster.Dex * 8 * damageMonster.Dmg;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Bite'n Shake")]
public class BiteAndShake(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffBeagsuain();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 14 + damageMonster.Dex * 10;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Corrosive Touch")]
public class CorrosiveTouch(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffCorrosiveTouch();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 15 + damageMonster.Con * 12;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Stomp")]
public class Stomp(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 18;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Head Butt")]
public class HeadButt(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 15 + damageMonster.Dex * 4 * damageMonster.Hit;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Claw")]
public class Claw(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetHorizontalInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 20 + damageMonster.Con * 15 * damageMonster.Dmg;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Mule Kick")]
public class MuleKick(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 16 + damageMonster.Dex * 14;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tail Slap")]
public class TailSlap(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageable) return;
            var enemy = damageable.DamageableGetBehind();

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
            }

            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 20 + damageMonster.Dex * 20 * damageMonster.Dmg;
        if (damageMonster.Template.BaseName == "Bahamut")
            dmg *= 250;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Roll Over")]
public class RollOver(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffBleeding();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Con * 16 + damageMonster.Dex * 18;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tantalizing Gaze")]
public class TantalizingGaze(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffCharmed();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Int * 16 + damageMonster.Wis * 18;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Swallow Whole")]
public class SwallowWhole(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffBleeding();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Int * 16 + damageMonster.Wis * 18;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Howl'n Call")]
public class HowlAndCall(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (!(Generator.RandomPercentPrecise() >= 0.70)) return;

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

            casterMonster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }
}

[Script("Death From Above")]
public class DeathFromAbove(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(3).FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 15 + damageMonster.Str * 13;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Pounce")]
public class Pounce(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6).FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Dex * 19 + damageMonster.Str * 19 + damageMonster.Con * 19;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tentacle")]
public class Tentacle(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffBeagsuain();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(enemy, sprite, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 14 + damageMonster.Dex * 14 + damageMonster.Con * 8;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Omega Avoid")]
public class OmegaAvoid(Skill skill) : SkillScript(skill)
{
    private readonly Buff _buff1 = new buff_MorDion();
    private readonly Buff _buff2 = new buff_PerfectDefense();

    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        GlobalSkillMethods.ApplyPhysicalBuff(sprite, _buff1);
        GlobalSkillMethods.ApplyPhysicalBuff(sprite, _buff2);
    }
}

[Script("Omega Slash")]
public class OmegaSlash(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemyList = damageDealer.GetInFrontToSide();

            if (enemyList.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemyList.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 80 + damageMonster.Dex * 55 * damageMonster.Dmg;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Fire Wheel")]
public class FireWheel(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            if (sprite is not Damageable damageable) return;
            var enemyList = damageable.GetFiveByFourRectInFront();

            if (enemyList.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemyList.Where(i => sprite.Serial != i.Serial))
            {
                if (enemy is not Aisling aisling) continue;
                var dmgCalc = DamageCalc(sprite);
                var fireCalc = FireDamageCalc(sprite);
                aisling.ApplyElementalSkillDamage(sprite, fireCalc, ElementManager.Element.Fire, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(aisling, sprite, Skill, dmgCalc, _crit);
                aisling.SendAnimationNearby(223, aisling.Position);
            }

            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }

    private long FireDamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Int * 72 + damageMonster.Wis * 38;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 95 + damageMonster.Dex * 86;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Megaflare")]
public class Megaflare(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
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

        try
        {
            var nearby = GetObjects<Aisling>(sprite.Map, i => i != null && i.WithinRangeOf(sprite, 6)).ToList();

            if (nearby.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var (_, enemy) in nearby.Where(enemy => enemy.Value != null && enemy.Value.Serial != sprite.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                enemy.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, false);
                enemy.SendAnimationNearby(217, enemy.Position);
            }

            if (sprite is not Damageable damageable) return;
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            // ignored
        }
    }

    private static long DamageCalc(Sprite sprite)
    {
        if (sprite is not Monster damageMonster) return 0;
        var dmg = damageMonster.Str * 99 + damageMonster.Int * 99;
        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        return critCheck.Item2;
    }
}

[Script("Lava Armor")]
public class LavaArmor(Skill skill) : SkillScript(skill)
{
    private readonly Buff _buff1 = new buff_skill_reflect();
    private readonly Buff _buff2 = new buff_spell_reflect();

    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
    {
        // Monster skill
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        GlobalSkillMethods.ApplyPhysicalBuff(sprite, _buff1);
        GlobalSkillMethods.ApplyPhysicalBuff(sprite, _buff2);
    }
}