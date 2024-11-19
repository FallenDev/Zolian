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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Assail";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Assault";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Onslaught";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Clobber";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Clobber x2";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Thrust";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(2);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }


            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(2).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wallop";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Thrash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Double Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Throw";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(4);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }


            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = (ushort)thrown,
                    TargetId = i.Serial
                };

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Glaives" or "Daggers"))
            {
                OnFailed(aisling);
                return;
            }

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.DamageableGetInFront(3).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var animation = new AnimationArgs
                {
                    SourceId = sprite.Serial,
                    TargetId = _target.Serial,
                    SourceAnimation = 10011,
                    TargetAnimation = 10011,
                    AnimationSpeed = 100
                };

                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Dex * 5 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 6);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Dex * 4 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 4);
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Aim";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = (ushort)thrown,
                    TargetId = i.Serial
                };

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Bows"))
            {
                OnFailed(aisling);
                return;
            }

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.DamageableGetInFront(5).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    SourceId = sprite.Serial,
                    TargetAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    TargetId = _target.Serial
                };

                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 4 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Dex * 3 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 3);
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Long Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(3).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(3).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Tiger Palm";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
            var hp = aisling.MaximumHp * 0.005;
            aisling.CurrentHp += (long)hp;

            if (aisling.CurrentHp >= aisling.MaximumHp)
                aisling.CurrentHp = aisling.MaximumHp;

            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Kenjutsu";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemies = aisling.GetInFrontToSide();

            foreach (var enemy in enemies)
            {
                _target = enemy;

                if (_target == null || _target.Serial == aisling.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                    OnFailed(aisling);
                    return;
                }

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
            }
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemies = identified.GetInFrontToSide();

                foreach (var enemy in enemies)
                {
                    _target = enemy;

                    if (_target == null || _target.Serial == sprite.Serial) continue;

                    var dmgCalc = DamageCalc(sprite);
                    GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
                }
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Short Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemies = aisling.GetInFrontToSide();

            foreach (var enemy in enemies)
            {
                _target = enemy;

                if (_target == null || _target.Serial == aisling.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                    OnFailed(aisling);
                    return;
                }

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
            }
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemies = identified.GetInFrontToSide();

                foreach (var enemy in enemies)
                {
                    _target = enemy;

                    if (_target == null || _target.Serial == sprite.Serial) continue;

                    var dmgCalc = DamageCalc(sprite);
                    GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
                }
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Mordhau";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemies = aisling.GetInFrontToSide();

            foreach (var enemy in enemies)
            {
                _target = enemy;

                if (_target == null || _target.Serial == aisling.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                    OnFailed(aisling);
                    return;
                }

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
            }
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemies = identified.GetInFrontToSide();

                foreach (var enemy in enemies)
                {
                    _target = enemy;

                    if (_target == null || _target.Serial == sprite.Serial) continue;

                    var dmgCalc = DamageCalc(sprite);
                    GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
                }
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Crushing Mace";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemies = aisling.GetInFrontToSide();

            foreach (var enemy in enemies)
            {
                _target = enemy;

                if (_target == null || _target.Serial == aisling.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                    OnFailed(aisling);
                    return;
                }

                var dmgCalc = DamageCalc(sprite);

                if (aisling.EquipmentManager.Equipment[1]?.Item != null)
                {
                    if (aisling.EquipmentManager.Equipment[1].Item.Template.Group == "Maces")
                    {
                        dmgCalc *= 2;
                    }
                }

                GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
            }
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemies = identified.GetInFrontToSide();

                foreach (var enemy in enemies)
                {
                    _target = enemy;

                    if (_target == null || _target.Serial == sprite.Serial) continue;

                    var dmgCalc = DamageCalc(sprite);
                    GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
                }
            }
            catch
            {
                // ignored
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
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
            var enemy = aisling.GetInFrontToSide();
            var enemy2 = aisling.GetInFrontToSide(2);
            enemy.AddRange(enemy2);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
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

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial) return;

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
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