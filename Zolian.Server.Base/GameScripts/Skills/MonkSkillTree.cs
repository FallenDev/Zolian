using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Ambush")]
public class Ambush(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite) { }

    protected override async void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Ambush";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var targetPos = damageDealer.GetFromAllSidesEmpty(target);

            if (target == null || target.Serial == sprite.Serial || targetPos == target.Position)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            var stepped = await damageDealer.StepAndRemove(damageDealer, targetPos.X, targetPos.Y);

            if (!stepped)
            {
                OnFailed(sprite, target);
                return;
            }

            damageDealer.Facing(target.X, target.Y, out var direction);
            damageDealer.Direction = (byte)direction;
            damageDealer.StepAddAndUpdateDisplay(damageDealer);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, dmgCalc, _crit, action);
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
        if (sprite is not Aisling aisling) return;
        Target(aisling);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = Skill.Level * 2;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Aisling aisling)
    {
        var client = aisling.Client;

        try
        {
            var _enemyList = client.Aisling.DamageableGetInFront(3).ToArray();
            var _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                OnFailed(aisling, null);
            }
            else
            {
                var success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (success)
                {
                    OnSuccess(aisling, _target);
                }
                else
                {
                    OnFailed(aisling, _target);
                }
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Wolf Fang Fist")]
public class WolfFangFist(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Wolf Fang Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
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
                OnFailed(sprite, null);
                return;
            }

            if (enemy.HasDebuff("Beag Suain"))
                enemy.RemoveDebuff("Beag Suain");

            var debuff = new DebuffFrozen();
            GlobalSkillMethods.ApplyPhysicalDebuff(sprite, debuff, enemy, Skill);
            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, 0, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Knife Hand Strike")]
public class KnifeHandStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Knife Hand Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
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
                OnFailed(sprite, null);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, dmgCalc, _crit, action);
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
            dmg = client.Aisling.Dex * 7 + client.Aisling.Str * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Palm Heel Strike")]
public class PalmHeelStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Palm Heel Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront();
            var enemy2 = damageDealer.DamageableGetBehind();
            enemy.AddRange(enemy2);

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
            dmg = client.Aisling.Con * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Hammer Twist")]
public class HammerTwist(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Hammer Twist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
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
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Dex * 1;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Dex * 1;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Cross Body Punch")]
public class CrossBodyPunch(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Cross Body Punch";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
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
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
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
            var imp = 50 + Skill.Level;
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

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Hurricane Kick")]
public class HurricaneKick(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Hurricane Kick";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
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
                var debuff = new DebuffHurricane();

                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, i, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Con * 6;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Con * 6;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Kelberoth Strike")]
public class Kelberoth_Strike(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Kelberoth Strike";
            aisling.Client.SendPublicMessage(aisling.Serial, PublicMessageType.Shout, $"{aisling.Username}: Ahhhhh!");
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.JumpAttack,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();
            var criticalHp = (long)(damageDealer.MaximumHp * .33);
            var kelbHp = (long)(damageDealer.CurrentHp * .66);

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(damageDealer, null);
                return;
            }

            var dmg = (long)(criticalHp * 2.5);

            if (damageDealer is Aisling aislingHp)
            {
                aislingHp.CurrentHp = kelbHp >= aislingHp.CurrentHp ? 1 : kelbHp;
                aislingHp.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel drained...");
                aislingHp.Client.SendAttributes(StatUpdateType.Vitality);
            }

            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, dmg, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Krane Kick")]
public class Krane_Kick(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Krane Kick";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Kick,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(2);

            if (enemy.Count == 0)
            {
                OnFailed(damageDealer, null);
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
            dmg = client.Aisling.Con * 4 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 4;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Claw Fist")]
public class Claw_Fist(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to enhance assails.");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Claw Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 70,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;

        if (damageDealer.HasBuff("Claw Fist"))
        {
            OnFailed(damageDealer, null);
            return;
        }

        var buff = new buff_clawfist();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        GlobalSkillMethods.OnSuccess(damageDealer, damageDealer, Skill, 0, false, action);
    }
}

[Script("Ember Strike")]
public class EmberStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Ember Strike";

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
            var enemy = damageDealer.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 3);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Fire, Skill);
                damageDealer.SendAnimationNearby(17, null, i.Serial);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, 0, _crit);
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
            dmg = client.Aisling.Con * 9;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 9;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Pummel")]
public class Pummel(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Pummel";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 8;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 8;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Thump")]
public class Thump(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Thump";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
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

            if (enemy.HasDebuff("Beag Suain"))
                enemy.RemoveDebuff("Beag Suain");

            if (enemy.HasDebuff("Frozen"))
                enemy.RemoveDebuff("Frozen");

            var debuff = new DebuffAdvFrozen();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, damageDealer.Int * damageDealer.Hit, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Eye Gouge")]
public class EyeGouge(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Eye Gouge";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
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
                OnFailed(sprite, null);
                return;
            }

            var debuff = new DebuffBlind();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, damageDealer.Str * damageDealer.Dmg, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Calming Mist")]
public class Calming_Mist(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Calming Mist";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter /= 4;
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Peace,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;
        damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, damageDealer.Serial);
        damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }
}

[Script("Healing Palms")]
public class HealingPalms(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Healing Palms";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            var dmgCalc = DamageCalc(sprite);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i.CurrentHp <= 1) continue;
                i.CurrentHp += dmgCalc;

                if (i.CurrentHp > i.MaximumHp)
                    i.CurrentHp = i.MaximumHp;

                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, i.Serial);

                if (i is Aisling targetPlayer)
                    targetPlayer.Client.SendAttributes(StatUpdateType.Vitality);
            }

            damageDealer.CurrentHp += dmgCalc;

            if (damageDealer.CurrentHp > damageDealer.MaximumHp)
                damageDealer.CurrentHp = damageDealer.MaximumHp;

            if (damageDealer is Aisling damageAisling)
                damageAisling.Client.SendAttributes(StatUpdateType.Vitality);

            GlobalSkillMethods.OnSuccess(damageDealer, damageDealer, Skill, 0, false, action);
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
        if (sprite is not Aisling aisling) return;
        var manaReq = aisling.MaximumHp * .10;

        if (aisling.CurrentMp >= manaReq)
        {
            aisling.CurrentMp -= (long)manaReq;
            OnSuccess(aisling);
            return;
        }

        OnFailed(aisling, null);
    }

    private long DamageCalc(Sprite sprite)
    {
        long dmg = 0;
        if (sprite is not Aisling damageDealingAisling) return dmg;
        var client = damageDealingAisling.Client;
        var imp = 50 + Skill.Level;
        dmg = client.Aisling.Wis * 6 + client.Aisling.Con * 6;
        dmg += dmg * imp / 100;
        return dmg;
    }
}

[Script("Ninth Gate")]
public class NinthGate(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Ninth Gate";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;
        var buff = new buff_ninthGate();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        GlobalSkillMethods.OnSuccess(damageDealer, damageDealer, Skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Drunken Fist")]
public class DrunkenFist(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Drunken Fist";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp2,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;
        var buff = new buff_drunkenFist();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        GlobalSkillMethods.OnSuccess(damageDealer, damageDealer, Skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}