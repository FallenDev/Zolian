using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public class GlobalSkillMethods
{
    private static bool Attempt(WorldClient client, Skill skill)
    {
        try
        {
            if (client.Aisling.CantAttack) return false;

            var success = Generator.RandNumGen100();

            if (skill.Level == 100)
            {
                return success >= 2;
            }

            return success switch
            {
                <= 25 when skill.Level <= 29 => false,
                <= 15 when skill.Level <= 49 => false,
                <= 10 when skill.Level <= 74 => false,
                <= 5 when skill.Level <= 99 => false,
                _ => true
            };
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
            var spriteX = spritePos.X;
            var spriteY = spritePos.Y;
            var inputX = inputPos.X;
            var inputY = inputPos.Y;
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

    public static void ApplyPhysicalDebuff(WorldClient client, Debuff debuff, Sprite target, Skill skill)
    {
        try
        {
            if (client != null)
            {
                var dmg = 0;

                if (!debuff.Name.Contains("Beag Suain"))
                {
                    var knockOutDmg = Generator.RandNumGen100();

                    if (knockOutDmg >= 98)
                    {
                        dmg += knockOutDmg * client.Aisling.Str * 3;
                    }
                    else
                    {
                        dmg += knockOutDmg * client.Aisling.Str * 1;
                    }

                    if (target is not Damageable damageable) return;
                    damageable.ApplyDamage(client.Aisling, dmg, skill);
                }
            }

            if (client != null)
                client.EnqueueDebuffAppliedEvent(target, debuff);
            else if (target is Aisling targetPlayer)
                targetPlayer.Client.EnqueueDebuffAppliedEvent(target, debuff);
            else
                debuff.OnApplied(target, debuff);

            if (target is not Monster) return;
            var animationPick = client?.Aisling.Path == Class.Defender
                ? client.Aisling.UsingTwoHanded
                    ? BodyAnimation.Swipe
                    : BodyAnimation.TwoHandAtk
                : BodyAnimation.Assail;

            client?.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(client.Aisling.Serial, animationPick, 20));
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
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendBodyAnimation(aisling.Serial, animationPick, 20));
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

    public static void Step(Sprite sprite, int savedXStep, int savedYStep)
    {
        try
        {
            if (sprite is not Aisling damageDealingSprite) return;
            var warpPos = new Position(savedXStep, savedYStep);
            damageDealingSprite.Client.WarpTo(warpPos);
            damageDealingSprite.Client.CheckWarpTransitions(damageDealingSprite.Client, savedXStep, savedYStep);
            damageDealingSprite.Client.SendRemoveObject(damageDealingSprite.Serial);
            damageDealingSprite.Client.UpdateDisplay();
            damageDealingSprite.Client.LastMovement = DateTime.UtcNow;
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with Step called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with Step called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
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
                if (client.Aisling.Buffs.TryRemove("Hide", out var hide))
                {
                    hide.OnEnded(client.Aisling, hide);
                }

                if (client.Aisling.Buffs.TryRemove("Shadowfade", out var shadowFade))
                {
                    shadowFade.OnEnded(client.Aisling, shadowFade);
                }

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
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, enemy.Serial));
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
            skill.LastUsedSkill = DateTime.UtcNow;
            if (!crit) return;
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
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
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, enemy.Serial));
            skill.LastUsedSkill = DateTime.UtcNow;
            if (!crit) return;
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
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
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, attacker.Serial));
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

    public static void FailedAttemptBodyAnimation(Sprite sprite, BodyAnimationArgs action)
    {
        try
        {
            if (sprite is not Aisling aisling) return;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
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
            if (sprite is not Aisling aisling) return;
            if (target is null) return;
            if (aisling.NextTo(target.X, target.Y))
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, target.Serial));
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