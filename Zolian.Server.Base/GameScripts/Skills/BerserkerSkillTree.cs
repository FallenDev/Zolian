using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Monsters;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Wind Slice")]
public class Wind_Slice(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Wind Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Dual Slice")]
public class Dual_Slice(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Dual Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
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

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Blitz")]
public class Blitz(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite) { }

    protected override async void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Blitz";

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
            dmg = (int)(client.Aisling.Str * 1 + client.Aisling.Dex * 1.3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 1 + damageMonster.Dex * 2;
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

[Script("Aid")]
public class Aid(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Aid";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter += aisling.Dex * 100000;
        }

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
            var enemy = damageDealer.DamageableGetInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy)
            {
                if (i is not Damageable damageable) continue;
                if (damageable is Aisling { Skulled: true } savedAisling)
                {
                    savedAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                    if (debuff != null)
                    {
                        debuff.Cancelled = true;
                        debuff.OnEnded(savedAisling, debuff);
                    }

                    savedAisling.Client.Revive();
                }

                if (damageable.HasDebuff("Beag Suain"))
                    damageable.Debuffs.TryRemove("Beag Suain", out _);

                if (damageable.HasDebuff("Silence"))
                    damageable.Debuffs.TryRemove("Silence", out _);

                damageable.ApplyDamage(damageDealer, 0, Skill);
                damageable.SendAnimationNearby(Skill.Template.TargetAnimation, null, damageable.Serial);
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
        if (sprite is not Aisling aisling) return;
        if (!Skill.CanUse()) return;

        var success = Generator.RandNumGen100();

        if (success <= 5)
        {
            OnFailed(aisling, null);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Lullaby Strike")]
public class Lullaby_Strike(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate!");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Lullaby Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
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

[Script("Desolate")]
public class Desolate(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Desolate";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 6;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Crasher")]
public class Crasher(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Crasher";
            aisling.Client.SendPublicMessage(aisling.Serial, PublicMessageType.Shout, $"{aisling.Username}: Errahhhh!");
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
            var criticalHp = (long)(damageDealer.MaximumHp * .95);
            var crasherHp = (long)(damageDealer.CurrentHp * .05);

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            var dmg = (long)(criticalHp * 1.5);

            if (damageDealer is Aisling aislingHp)
            {
                aislingHp.CurrentHp = crasherHp >= aislingHp.CurrentHp ? 1 : crasherHp;
                aislingHp.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel drained...");
                aislingHp.Client.SendAttributes(StatUpdateType.Vitality);
            }

            GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmg, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Sever")]
public class Sever(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Sever";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
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
            var imp = 50 + Skill.Level;
            dmg = (int)(client.Aisling.Str * 5 + client.Aisling.Dex * 1.5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Rush")]
public class Rush(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        GlobalSkillMethods.OnFailed(sprite, Skill, target);
    }

    protected override void OnSuccess(Sprite sprite) { }

    protected override async void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Rush";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        if (target == null)
        {
            OnFailed(sprite, null);
            return;
        }

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var dmgCalc = DamageCalc(damageDealer);
            var position = target.Position;
            var mapCheck = damageDealer.Map.ID;
            var wallPosition = damageDealer.GetPendingChargePosition(3, damageDealer);
            var targetPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, position);
            var wallPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, wallPosition);

            if (mapCheck != damageDealer.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (damageDealer.Direction)
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

                if (damageDealer.Position != position)
                {
                    var stepped = await damageDealer.StepAndRemove(damageDealer, position.X, position.Y);

                    if (!stepped)
                    {
                        OnFailed(sprite, null);
                        return;
                    }

                    damageDealer.StepAddAndUpdateDisplay(damageDealer);
                }

                if (target is not Damageable damageable) return;
                damageable.ApplyDamage(damageDealer, dmgCalc, Skill);
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, target.Serial);

                if (!_crit) return;
                damageable.SendAnimationNearby(387, null, sprite.Serial);
            }
            else
            {
                var stepped = await damageDealer.StepAndRemove(damageDealer, wallPosition.X, wallPosition.Y);

                if (!stepped)
                {
                    OnFailed(sprite, null);
                    return;
                }

                damageDealer.StepAddAndUpdateDisplay(damageDealer);
                var stunned = new DebuffBeagsuain();
                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, stunned, damageDealer, Skill);
                damageDealer.SendAnimationNearby(208, null, damageDealer.Serial);
            }
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
        if (sprite is not Damageable damageDealer) return;

        if (damageDealer.CantAttack)
        {
            OnFailed(damageDealer, null);
            return;
        }

        Target(damageDealer);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = Skill.Level * 2;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private async void Target(Sprite sprite)
    {
        if (sprite is not Damageable damageable) return;

        try
        {
            var _enemyList = damageable.DamageableGetInFront(3).ToArray();
            var _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                var mapCheck = damageable.Map.ID;
                var wallPosition = damageable.GetPendingChargePositionNoTarget(3, damageable);
                var wallPos = GlobalSkillMethods.DistanceTo(damageable.Position, wallPosition);

                if (mapCheck != damageable.Map.ID) return;
                if (!(wallPos > 0))
                {
                    OnFailed(damageable, null);
                    return;
                }

                if (damageable.Position != wallPosition)
                {
                    var stepped = await damageable.StepAndRemove(damageable, wallPosition.X, wallPosition.Y);

                    if (!stepped)
                    {
                        OnFailed(sprite, null);
                        return;
                    }

                    damageable.StepAddAndUpdateDisplay(damageable);
                }

                if (wallPos <= 2)
                {
                    var stunned = new DebuffBeagsuain();
                    GlobalSkillMethods.ApplyPhysicalDebuff(damageable, stunned, damageable, Skill);
                    damageable.SendAnimationNearby(208, null, damageable.Serial);
                }

                if (damageable is Aisling skillUsed)
                    skillUsed.UsedSkill(Skill);
            }
            else
            {
                OnSuccess(sprite, _target);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Titan's Cleave")]
public class Titans_Cleave(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Titan's Cleave";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
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
                var debuff = new DebuffTitansCleave();
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
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += (int)(damageMonster.Con * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Retribution")]
public class Retribution(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Retribution";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            var enemyTwo = damageDealer.DamageableGetBehind();
            enemy.Add(enemyTwo.FirstOrDefault());

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                var debuff = new DebuffRetribution();

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
            dmg = client.Aisling.Str * 9 + client.Aisling.Dex * 3 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 9;
            dmg += damageMonster.Con * 3 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Sneak Attack")]
public class Sneak_Attack(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite) { }

    protected override async void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Sneak Attack";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Jump,
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

            var dmgCalc = DamageCalc(damageDealer);
            var stepped = await damageDealer.StepAndRemove(damageDealer, targetPos.X, targetPos.Y);

            if (!stepped)
            {
                OnFailed(sprite, null);
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

    private void Target(Sprite sprite)
    {
        if (sprite is not Damageable damageDealingSprite) return;

        try
        {
            var _enemyList = damageDealingSprite.DamageableGetInFront().ToArray();
            var _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                OnFailed(damageDealingSprite, null);
            }
            else if (damageDealingSprite is Aisling aisling)
            {
                var success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (success)
                {
                    OnSuccess(damageDealingSprite, _target);
                }
                else
                {
                    OnFailed(damageDealingSprite, _target);
                }
            }
            else
            {
                OnSuccess(damageDealingSprite, _target);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Berserk")]
public class Berserk(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to enrage");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Berserk";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var buff = new buff_berserk();
        GlobalSkillMethods.ApplyPhysicalBuff(sprite, buff);
        GlobalSkillMethods.OnSuccess(sprite, sprite, Skill, 0, false, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        OnSuccess(sprite);
    }
}