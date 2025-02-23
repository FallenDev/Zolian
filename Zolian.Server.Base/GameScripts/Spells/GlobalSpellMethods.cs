using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

public class GlobalSpellMethods
{
    private const int CritDmg = 2;

    public static bool Execute(WorldClient client, Spell spell)
    {
        try
        {
            if (client.Aisling.CantCast)
            {
                if (spell.Template.Name is not ("Ao Suain" or "Ao Sith"))
                {
                    return false;
                }
            }

            var success = Generator.RandNumGen100();

            const int minEffective = 5;
            const int maxEffective = 100;
            const int requiredRollAtMin = 30;
            const int requiredRollAtMax = 5;

            // Clamp the level for interpolation.
            var effectiveLevel = Math.Clamp((int)spell.Level, minEffective, maxEffective);

            // Compute the fraction within the effective range.
            var fraction = (effectiveLevel - minEffective) / (double)(maxEffective - minEffective);

            // Linearly interpolate the required roll.
            var requiredRoll = (int)Math.Ceiling(requiredRollAtMin + (requiredRollAtMax - requiredRollAtMin) * fraction);

            return success >= requiredRoll;
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"Issue with Attempt called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with Attempt called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

        return true;
    }

    private static bool CritStrike()
    {
        var critRoll = Generator.RandNumGen100();
        {
            return critRoll >= 95;
        }
    }

    public static void Train(WorldClient client, Spell spell) => client.TrainSpell(spell);

    public static long WeaponDamageElementalProc(Sprite sprite, int weaponProc)
    {
        if (sprite is not Aisling damageDealingAisling) return 0;
        var client = damageDealingAisling.Client;
        var level = damageDealingAisling.Level;
        var dmg = client.Aisling.Int * client.Aisling.Str * weaponProc;

        var levelBuff = level switch
        {
            <= 29 => 1,
            <= 49 => 1.2,
            <= 69 => 1.4,
            <= 98 => 1.8,
            _ => 2
        };

        dmg += (int)levelBuff * dmg;

        return dmg;
    }

    public static long AislingSpellDamageCalc(Sprite sprite, long baseDmg, Spell spell, double exp)
    {
        const int dmg = 0;
        if (sprite is not Aisling damageDealingAisling) return dmg;
        var spellLevelOffset = spell.Level + 1;
        var client = damageDealingAisling.Client;
        var bonus = baseDmg + 2.0 * spellLevelOffset;
        var amp = client.Aisling.Int / 2.0 * exp;
        var final = (int)(amp + bonus);
        var crit = CritStrike();

        if (!crit) return final;

        damageDealingAisling.SendAnimationNearby(387, null, damageDealingAisling.Serial);
        final *= CritDmg;

        return final;
    }

    public static long MonsterElementalDamageProc(Sprite sprite, long baseDmg, Spell spell, double exp)
    {
        if (sprite is not Monster damageMonster) return 0;
        var imp = baseDmg + 2.0;
        var level = damageMonster.Level;

        var amp = damageMonster.Int / 2.0 * exp;
        var final = (int)(amp + imp) + level;
        var crit = CritStrike();

        if (!crit) return final;

        damageMonster.SendAnimationNearby(387, null, damageMonster.Serial);
        final *= CritDmg;

        return final;
    }

    public static void ElementalOnSuccess(Sprite sprite, Sprite target, Spell spell, double exp)
    {
        if (target is not Damageable damageable) return;
        if (sprite is Aisling aisling)
        {
            var levelSeed = (long)((aisling.ExpLevel + aisling.AbpLevel) * 0.10 * spell.Level);
            var dmg = AislingSpellDamageCalc(sprite, levelSeed, spell, exp);

            if (target.CurrentHp > 0)
            {
                aisling.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
                damageable.ApplyElementalSpellDamage(aisling, dmg, spell.Template.ElementalProperty, spell);
            }
            else
            {
                aisling.SendAnimationNearby(spell.Template.TargetAnimation, target.Position);
            }
        }
        else
        {
            var dmg = (long)damageable.GetBaseDamage(sprite, sprite.Target, MonsterEnums.Elemental);
            dmg = MonsterElementalDamageProc(sprite, dmg, spell, exp);
            damageable.ApplyElementalSpellDamage(sprite, dmg, spell.Template.ElementalProperty, spell);

            if (target is Aisling targetAisling)
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} casts {spell.Template.Name} elemental on you");

            damageable.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
        }
    }

    public void ElementalOnUse(Sprite sprite, Sprite target, Spell spell, double exp = 1)
    {
        if (target == null) return;
        if (!spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(184, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendAnimationNearby(184, null, target.Serial);
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(64, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendAnimationNearby(64, null, target.Serial);
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {spell.Template.Name}");
            return;
        }

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                if (aisling.CurrentMp - spell.Template.ManaCost > 0)
                {
                    aisling.CurrentMp -= spell.Template.ManaCost;
                    Train(client, spell);
                }
                else
                {
                    client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                    return;
                }

                var success = Execute(client, spell);

                if (success)
                {
                    ElementalOnSuccess(aisling, target, spell, exp);
                }
                else
                {
                    SpellOnFailed(aisling, target, spell);
                }

                client.SendAttributes(StatUpdateType.Vitality);
            }
            else
            {
                if (sprite.CurrentMp - spell.Template.ManaCost > 0)
                {
                    sprite.CurrentMp -= spell.Template.ManaCost;
                }
                else
                {
                    SpellOnFailed(sprite, target, spell);
                    return;
                }

                if (sprite.CurrentMp < 0)
                    sprite.CurrentMp = 0;

                ElementalOnSuccess(sprite, target, spell, exp);
            }
        }
        else
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(115, null, target.Serial);
                SpellOnFailed(sprite, target, spell);
                return;
            }

            SpellOnFailed(sprite, target, spell);
        }
    }

    public static void ElementalNecklaceOnSuccess(Sprite sprite, Sprite target, Spell spell, double exp)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Damageable damageable) return;
        var levelSeed = (long)((aisling.ExpLevel + aisling.AbpLevel) * 0.10 * spell.Level);
        var dmg = AislingSpellDamageCalc(sprite, levelSeed, spell, exp);

        if (target.CurrentHp > 0)
        {
            aisling.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.ApplyElementalSpellDamage(aisling, dmg, aisling.OffenseElement, spell);
        }
        else
        {
            aisling.SendAnimationNearby(spell.Template.TargetAnimation, target.Position);
        }
    }

    public void ElementalNecklaceOnUse(Sprite sprite, Sprite target, Spell spell, double exp = 1)
    {
        if (target == null) return;
        if (!spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(184, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendAnimationNearby(184, null, target.Serial);
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(64, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendAnimationNearby(64, null, target.Serial);
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {spell.Template.Name}");
            return;
        }

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (sprite is not Aisling aisling) return;
            var client = aisling.Client;

            if (aisling.CurrentMp - spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= spell.Template.ManaCost;
                Train(client, spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            var success = Execute(client, spell);

            if (success)
            {
                ElementalNecklaceOnSuccess(aisling, target, spell, exp);
            }
            else
            {
                SpellOnFailed(aisling, target, spell);
            }

            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite is not Aisling caster) return;
            caster.SendAnimationNearby(115, null, target.Serial);
            SpellOnFailed(sprite, target, spell);
        }
    }

    public static void AfflictionOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
    {
        if (target is not Damageable damageable) return;

        if (sprite is Aisling aisling)
        {
            if (target.CurrentHp > 0)
            {
                if (target is Aisling targetPlayer)
                    targetPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username} afflicts you with {spell.Template.Name}");

                aisling.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendAnimationNearby(spell.Template.TargetAnimation, target.Position);
            }

            aisling.Client.EnqueueDebuffAppliedEvent(target, debuff);
        }
        else
        {
            if (target is Aisling targetAisling)
            {
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} afflicts you with {spell.Template.Name}");
                targetAisling.Client.EnqueueDebuffAppliedEvent(target, debuff);
            }
            else
                debuff.OnApplied(target, debuff);

            damageable.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public static void PoisonOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
    {
        if (target is not Damageable damageable) return;
        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") ||
            target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein")) return;

        if (sprite is Aisling aisling)
        {
            if (target.CurrentHp > 0)
            {
                if (target is Aisling targetPlayer)
                    targetPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username} poisons you with {spell.Template.Name}.");

                aisling.SendAnimationNearby(spell.Template.Animation, null, target.Serial);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendAnimationNearby(spell.Template.Animation, target.Position);
            }

            aisling.Client.EnqueueDebuffAppliedEvent(target, debuff);
        }
        else
        {
            if (target is Aisling targetAisling)
            {
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} poisoned you with {spell.Template.Name}");
                targetAisling.Client.EnqueueDebuffAppliedEvent(target, debuff);
            }
            else
                debuff.OnApplied(target, debuff);

            damageable.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public static void SpellOnSuccess(Sprite sprite, Sprite target, Spell spell)
    {
        if (target is not Damageable damageable) return;

        if (target is Aisling targetAisling)
            targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} cast {spell.Template.Name} on you");

        if (target.CurrentHp > 0)
        {
            damageable.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
        else
        {
            damageable.SendAnimationNearby(spell.Template.TargetAnimation, target.Position);
        }

        if (sprite is not Monster) return;
        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
    }

    public static void SpellOnFailed(Sprite sprite, Sprite target, Spell spell)
    {
        if (target is not Damageable damageable) return;
        damageable.SendAnimationNearby(115, null, target.Serial, 50);

        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{spell.Template.Name} has failed.");
    }

    public void AfflictionOnUse(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
    {
        if (target == null) return;
        if (!spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(184, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendAnimationNearby(184, null, target.Serial);
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(64, null, target.Serial);
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendAnimationNearby(64, null, target.Serial);
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {spell.Template.Name}");
            return;
        }

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                if (aisling.CurrentMp - spell.Template.ManaCost > 0)
                {
                    aisling.CurrentMp -= spell.Template.ManaCost;
                    Train(client, spell);
                }
                else
                {
                    client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                    return;
                }

                var success = Execute(client, spell);

                if (success)
                {
                    if (debuff.Name.Contains("Puinsein"))
                    {
                        PoisonOnSuccess(aisling, target, spell, debuff);
                    }
                    else
                    {
                        AfflictionOnSuccess(aisling, target, spell, debuff);
                    }
                }
                else
                {
                    SpellOnFailed(aisling, target, spell);
                }

                client.SendAttributes(StatUpdateType.Vitality);
            }
            else
            {
                if (sprite.CurrentMp - spell.Template.ManaCost > 0)
                {
                    sprite.CurrentMp -= spell.Template.ManaCost;
                }
                else
                {
                    SpellOnFailed(sprite, target, spell);
                    return;
                }

                if (sprite.CurrentMp < 0)
                    sprite.CurrentMp = 0;


                if (debuff.Name.Contains("Puinsein"))
                {
                    PoisonOnSuccess(sprite, target, spell, debuff);
                }
                else
                {
                    AfflictionOnSuccess(sprite, target, spell, debuff);
                }
            }
        }
        else
        {
            if (sprite is Aisling caster)
            {
                caster.SendAnimationNearby(115, null, target.Serial);
                SpellOnFailed(sprite, target, spell);
                return;
            }

            SpellOnFailed(sprite, target, spell);
        }
    }

    public static void EnhancementOnSuccess(Sprite sprite, Sprite target, Spell spell, Buff buff)
    {
        if (target is not Damageable damageable) return;

        if (sprite is Aisling aisling)
        {
            if (target.CurrentHp > 0)
            {
                aisling.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendAnimationNearby(spell.Template.TargetAnimation, target.Position);
            }

            aisling.Client.EnqueueBuffAppliedEvent(target, buff);
        }
        else
        {
            if (target is Aisling targetPlayer)
            {
                if (!target.HasBuff(buff.Name))
                {
                    targetPlayer.Client.EnqueueBuffAppliedEvent(targetPlayer, buff);
                }
            }
            else
            {
                if (!target.HasBuff(buff.Name))
                    buff.OnApplied(sprite, buff);
            }

            damageable.SendAnimationNearby(spell.Template.TargetAnimation, null, target.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public static void EnhancementOnUse(Sprite sprite, Sprite target, Spell spell, Buff buff)
    {
        if (target == null) return;
        if (!spell.CanUse())
        {
            if (sprite is Aisling abilityCheck)
                abilityCheck.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - spell.Template.ManaCost > 0)
            {
                aisling.CurrentMp -= spell.Template.ManaCost;
                Train(client, spell);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            EnhancementOnSuccess(sprite, target, spell, buff);
            client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            if (sprite.CurrentMp - spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= spell.Template.ManaCost;
            }
            else
            {
                SpellOnFailed(sprite, target, spell);
                return;
            }

            EnhancementOnSuccess(sprite, target, spell, buff);
        }
    }

    public static void Step(Sprite sprite, int savedXStep, int savedYStep)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var warpPos = new Position(savedXStep, savedYStep);
        damageDealingSprite.Client.WarpTo(warpPos);
        damageDealingSprite.Client.CheckWarpTransitions(damageDealingSprite.Client, savedXStep, savedYStep);
        damageDealingSprite.Client.SendRemoveObject(damageDealingSprite.Serial);
        damageDealingSprite.Client.UpdateDisplay();
        damageDealingSprite.Client.LastMovement = DateTime.UtcNow;
    }
}