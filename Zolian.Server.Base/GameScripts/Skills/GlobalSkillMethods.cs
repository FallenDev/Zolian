using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public static class GlobalSkillMethods
{
    private static bool Attempt(WorldClient client, Skill skill)
    {
        try
        {
            if (client.Aisling.CantAttack) return false;
            if (skill.Template.MaxLevel == 1) return true;

            var success = Generator.RandNumGen100();

            const int minEffective = 5;
            const int maxEffective = 350;
            const int requiredRollAtMin = 30;
            const int requiredRollAtMax = 1;

            // Clamp the level for interpolation.
            var effectiveLevel = Math.Clamp(skill.Level, minEffective, maxEffective);

            // Compute the fraction within the effective range.
            var fraction = (effectiveLevel - minEffective) / (double)(maxEffective - minEffective);

            // Linearly interpolate the required roll.
            var requiredRoll = (int)Math.Ceiling(requiredRollAtMin + (requiredRollAtMax - requiredRollAtMin) * fraction);

            return success >= requiredRoll;
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with Attempt called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with Attempt called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return true;
    }

    public static int DistanceTo(Position spritePos, Position inputPos)
    {
        try
        {
            spritePos.GetSnapshot(out var spriteX, out var spriteY);
            inputPos.GetSnapshot(out var inputX, out var inputY);

            var diffX = Math.Abs(spriteX - inputX);
            var diffY = Math.Abs(spriteY - inputY);
            return diffX + diffY;
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with DistanceTo called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with DistanceTo called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return default;
    }

    public static void ApplyPhysicalDebuff(Sprite attacker, Debuff debuff, Sprite target, Skill skill)
    {
        try
        {
            if (target is not Damageable damageable) return;
            if (attacker is not Damageable damageDealer) return;

            var dmg = 0;

            if (debuff.Name.Contains("Suain"))
            {
                var knockOutDmg = Generator.RandNumGen100();

                if (knockOutDmg >= 98)
                {
                    dmg += knockOutDmg * damageable.Str * 13;
                }
                else
                {
                    dmg += knockOutDmg * damageable.Str * 3;
                }

                damageable.ApplyDamage(damageDealer, dmg, skill, true);
            }

            if (target.HasDebuff(debuff.Name))
                target.RemoveDebuff(debuff.Name);

            if (target is Aisling targetPlayer)
                targetPlayer.Client.EnqueueDebuffAppliedEvent(target, debuff);
            else
                debuff.OnApplied(target, debuff);

            damageDealer.SendAnimationNearby(skill.Template.TargetAnimation, null, target.Serial);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with ApplyPhysicalDeBuff called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with ApplyPhysicalDeBuff called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static void ApplyPhysicalBuff(Sprite target, Buff buff)
    {
        try
        {
            if (target is Aisling aisling)
            {
                var animationPick = aisling.Path == Class.Defender
                    ? aisling.UsingTwoHanded
                        ? BodyAnimation.Swipe
                        : BodyAnimation.TwoHandAtk
                    : BodyAnimation.Assail;

                aisling.Client.EnqueueBuffAppliedEvent(aisling, buff);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(aisling.Serial, animationPick, 20));
                return;
            }

            buff.OnApplied(target, buff);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with ApplyPhysicalBuff called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with ApplyPhysicalBuff called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static void Train(WorldClient client, Skill skill) => client?.TrainSkill(skill);

    public static bool OnUse(Aisling aisling, Skill skill)
    {
        var client = aisling.Client;

        try
        {
            aisling.UsedSkill(skill);

            if (client.Aisling.IsInvisible &&
                (skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.BreakInvisible) ||
                 skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.Both)))
            {
                aisling.RemoveBuff("Hide");
                aisling.RemoveBuff("Shadowfade");
                aisling.RemoveBuff("Blend");
                client.UpdateDisplay();
                return Attempt(client, skill);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {skill.Name} within GlobalSkillOnUse called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {skill.Name} within GlobalSkillOnUse called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return Attempt(client, skill);
    }

    public static void OnSuccess(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit, BodyAnimationArgs action)
    {
        try
        {
            if (attacker is Monster)
                action.BodyAnimation = BodyAnimation.Assail;

            var target = enemy;

            // Damage
            if (dmg > 0)
            {
                target = Skill.Reflect(target, attacker, skill);
                if (target is not Damageable damageable) return;
                damageable.ApplyDamage(attacker, dmg, skill);
            }

            // Training
            if (attacker is Aisling aisling)
                Train(aisling.Client, skill);

            if (attacker is not Damageable damageDealer) return;

            // Animation
            damageDealer.SendAnimationNearby(skill.Template.TargetAnimation, null, enemy.Serial);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
            skill.LastUsedSkill = DateTime.UtcNow;
            if (!crit) return;
            damageDealer.SendAnimationNearby(387, null, attacker.Serial);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {skill.Name} within GlobalSkillOnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {skill.Name} within GlobalSkillOnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static void OnSuccessWithoutAction(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit)
    {
        try
        {
            var target = enemy;

            // Damage
            if (dmg > 0)
            {
                target = Skill.Reflect(target, attacker, skill);
                if (target is not Damageable damageable) return;
                damageable.ApplyDamage(attacker, dmg, skill);
            }

            // Training
            if (attacker is Aisling aisling)
                Train(aisling.Client, skill);

            if (attacker is not Damageable damageDealer) return;

            // Animation
            damageDealer.SendAnimationNearby(skill.Template.TargetAnimation, null, enemy.Serial);
            skill.LastUsedSkill = DateTime.UtcNow;
            if (!crit) return;
            damageDealer.SendAnimationNearby(387, null, attacker.Serial);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {skill.Name} within GlobalSkillOnSuccessWithoutAction called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {skill.Name} within GlobalSkillOnSuccessWithoutAction called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static void OnSuccessWithoutActionAnimation(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit)
    {
        try
        {
            var target = enemy;

            // Damage
            if (dmg > 0)
            {
                target = Skill.Reflect(target, attacker, skill);
                if (target is not Damageable damageable) return;
                damageable.ApplyDamage(attacker, dmg, skill);
            }

            // Training
            if (attacker is Aisling aisling)
                Train(aisling.Client, skill);

            if (attacker is not Damageable damageDealer) return;

            skill.LastUsedSkill = DateTime.UtcNow;
            if (!crit) return;
            damageDealer.SendAnimationNearby(387, null, attacker.Serial);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {skill.Name} within GlobalSkillOnSuccessWithoutActionAnimation called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {skill.Name} within GlobalSkillOnSuccessWithoutActionAnimation called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static int Thrown(WorldClient client, Skill skill, bool crit)
    {
        try
        {
            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Glaives" or "Shuriken" or "Daggers" or "Bows")) return 10015;

            // 10006,7,8 = ice arrows, 10003,4,5 = fire arrows
            return client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group switch
            {
                "Glaives" => 10012,
                "Shuriken" => 10011,
                "Daggers" => 10009,
                "Bows" => crit ? 10002 : 10000,
                "Apple" => 10010,
                _ => 10015
            };
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with Thrown called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with Thrown called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return 10015;
    }

    public static void FailedAttemptBodyAnimation(Sprite sprite)
    {
        try
        {
            if (sprite is not Damageable damageable) return;
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 35));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with FailedAttemptBodyAnimation called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with FailedAttemptBodyAnimation called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static void OnFailed(Sprite sprite, Skill skill, Sprite target)
    {
        try
        {
            FailedAttemptBodyAnimation(sprite);
            if (target is null) return;
            if (sprite is not Damageable damageDealer) return;
            target.GetPositionSnapshot(out var targetX, out var targetY);

            if (damageDealer.NextTo(targetX, targetY))
                damageDealer.SendAnimationNearby(skill.Template.MissAnimation, null, target.Serial);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {skill.Name} within GlobalSkillOnFailed called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {skill.Name} within GlobalSkillOnFailed called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public static (bool, long) OnCrit(long dmg)
    {
        try
        {
            var critRoll = Generator.RandNumGen100();
            if (critRoll < 99) return (false, dmg);
            dmg *= 2;
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with OnCrit called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with OnCrit called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return (true, dmg);
    }
}