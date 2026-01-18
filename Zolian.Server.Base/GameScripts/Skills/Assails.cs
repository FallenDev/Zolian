using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Aisling Str * 2 | Monster Str * 2, Dex * 1.2
[Script("Assail")]
public class Assail(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Assail";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 2.5 | Monster Str * 2.5, Dex * 1.2
[Script("Assault")]
public class Assault(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Assault";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = (int)(client.Aisling.Str * 2.5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = (int)(damageMonster.Str * 2.5);
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex * 2.5 | Monster Dex * 2.5, Str * 1.2
[Script("Onslaught")]
public class Onslaught(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Onslaught";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = (int)(client.Aisling.Dex * 2.5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            dmg = (int)(damageMonster.Str * 1.2);
            dmg += (int)(damageMonster.Dex * 2.5);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 3 | Monster Str * 3, Dex * 1.2
[Script("Clobber")]
public class Clobber(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Clobber";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex * 3 | Monster Str * 1.2, Dex * 3
[Script("Clobber x2")]
public class ClobberX2(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Clobber x2";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(enemy, sprite, Skill, dmgCalc, _crit);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 3 | Monster Str * 3, Dex * 1.2
[Script("Thrust")]
public class Thrust(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Thrust";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(2);

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 4, Dex * 1.2 | Monster Str * 4, Dex * 1.2
[Script("Wallop")]
public class Wallop(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Wallop";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 4;
            dmg += (int)(client.Aisling.Dex * 1.2);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += (int)(damageMonster.Dex * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 5, Dex * 3 | Monster Str * 5, Dex * 3
[Script("Thrash")]
public class Thrash(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Thrash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 5;
            dmg += client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 2, Con * 2 | Monster Str * 2, Con * 2
[Script("Punch")]
public class Punch(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Con * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 4, Con * 3 | Monster Str * 4, Con * 3
[Script("Double Punch")]
public class DoublePunch(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Double Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, enemy);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccessWithoutActionAnimation(enemy, sprite, Skill, dmgCalc, _crit);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex * 5, * Distance Max 6 | Monster Dex * 4, * Distance Max 4
[Script("Throw")]
public class Throw(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
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
            var enemy = damageDealer.DamageableGetInFront(4);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (sprite is Aisling aisling)
                {
                    aisling.ActionUsed = "Throw";
                    var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)thrown,
                        SourceId = aisling.Serial,
                        TargetAnimation = (ushort)thrown,
                        TargetId = i.Serial
                    };
                }
                else
                {
                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = 10011,
                        SourceId = sprite.Serial,
                        TargetAnimation = 10011,
                        TargetId = i.Serial
                    };
                }

                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Glaives" or "Daggers" or "Shuriken"))
            {
                OnFailed(aisling, null);
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex * 5 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 6);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex * 4 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 4);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 2, Dex * 4, * Distance (Max 5) | Monster Str * 3, Dex * 3, * Distance (Max 3)
[Script("Aim")]
public class Aim(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (sprite is Aisling aisling)
                {
                    aisling.ActionUsed = "Aim";
                    var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)thrown,
                        SourceId = aisling.Serial,
                        TargetAnimation = (ushort)thrown,
                        TargetId = i.Serial
                    };
                }
                else
                {
                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)(_crit ? 10002 : 10000),
                        SourceId = sprite.Serial,
                        TargetAnimation = (ushort)(_crit ? 10002 : 10000),
                        TargetId = i.Serial
                    };
                }

                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Bows" or "Guns"))
            {
                OnFailed(aisling, null);
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 4 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Dex * 3 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str | Monster Str
[Script("Two-Handed Attack")]
public class TwoHandedAttack(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Two-handed Attack";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1]?.Item == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str | Monster Str
[Script("Kobudo")]
public class Kobudo(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Kobudo";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1]?.Item == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str | Monster Str
[Script("Advanced Staff Train")]
public class AdvancedStaffTraining(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Advanced Staff Training";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1]?.Item == null) return;
            if (!aisling.EquipmentManager.Equipment[1].Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff)) return;

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex | Monster Dex
[Script("Dual Wield")]
public class DualWield(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Dual Wield";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.DoubleStab,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[3]?.Item != null)
            {
                if (!aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.DualWield)) return;
            }
            else
            {
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex | Monster Dex
[Script("Ambidextrous")]
public class Ambidextrous(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Ambidextrous";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.DoubleStab,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[3]?.Item != null)
            {
                if (!aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.DualWield)) return;
            }
            else
            {
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 7, Dex * 1 | Monster Str * 7, Dex * 1
[Script("Long Strike")]
public class LongStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Long Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(3);

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 7 + client.Aisling.Dex * 1;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 7 + damageMonster.Dex * 1;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 2, Dex * 2, Int * 2  | Monster Str * 2, Dex * 2, Int * 2
[Script("Divine Thrust")]
public class DivineThrust(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Divine Thrust";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(3);

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

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Int * 2 + client.Aisling.Dex * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Int * 2 + damageMonster.Dex * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Wis * 4, Con * 4 | Monster Wis * 4, Con * 4
[Script("Tiger Palm")]
public class TigerPalm(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Tiger Palm";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
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
            var hp = sprite.MaximumHp * 0.005;
            sprite.CurrentHp += (long)hp;

            if (sprite.CurrentHp >= sprite.MaximumHp)
                sprite.CurrentHp = sprite.MaximumHp;

            if (sprite is Aisling vita)
                vita.Client.SendAttributes(StatUpdateType.Vitality);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Wis * 4 + client.Aisling.Con * 4;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Wis * 4 + damageMonster.Con * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Dex * 10, Str * 4 | Monster Dex * 8, Str * 2
[Script("Kenjutsu")]
public class Kenjutsu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Kenjutsu";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemies = damageDealer.GetInFrontToSide();

            if (enemies.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemies)
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex * 10 + client.Aisling.Str * 4;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex * 8 + damageMonster.Str * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 15 | Monster Str * 15
[Script("Short Strike")]
public class ShortStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Short Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemies = damageDealer.GetInFrontToSide();

            if (enemies.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemies)
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 15;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 15;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 7, Con * 7 | Monster Str * 7, Con * 4
[Script("Mordhau")]
public class Mordhau(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Mordhau";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemies = damageDealer.GetInFrontToSide();

            if (enemies.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemies)
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 7 + client.Aisling.Con * 7;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 7 + damageMonster.Con * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aisling Str * 7, Con * 7 | Monster Str * 7, Con * 4
[Script("Crushing Mace")]
public class CrushingMace(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Crushing Mace";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemies = damageDealer.GetInFrontToSide();

            if (enemies.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var enemy in enemies)
            {
                var dmgCalc = DamageCalc(sprite);
                if (sprite is Aisling weaponBoost && weaponBoost.EquipmentManager.Equipment[1]?.Item != null)
                {
                    if (weaponBoost.EquipmentManager.Equipment[1].Item.Template.Group == "Maces")
                    {
                        dmgCalc *= 2;
                    }
                }

                GlobalSkillMethods.OnSuccessWithoutAction(enemy, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 7 + client.Aisling.Con * 7;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 7 + damageMonster.Con * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Daisho")]
public class Daisho(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Daisho";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HeavySwipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            var enemy2 = damageDealer.GetInFrontToSide(2);
            enemy.AddRange(enemy2);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, sprite, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            if (aisling.EquipmentManager.Equipment[1]?.Item == null) return;
            if (aisling.EquipmentManager.Equipment[1]?.Item?.Template?.DmgMin == 0 || aisling.EquipmentManager.Equipment[3]?.Item?.Template?.DmgMin == 0)
            {
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 40 + client.Aisling.Dex * 60;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}