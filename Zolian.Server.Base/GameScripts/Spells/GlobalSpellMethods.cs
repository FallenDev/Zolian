using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

public class GlobalSpellMethods : IGlobalSpellMethods
{
    private const int CritDmg = 2;

    public bool Execute(WorldClient client, Spell spell)
    {
        if (client.Aisling.CantCast) return false;
        var success = Generator.RandNumGen100();

        if (spell.Level == 100)
        {
            return success >= 3;
        }

        return success switch
        {
            <= 20 when spell.Level <= 29 => false,
            <= 10 when spell.Level <= 49 => false,
            <= 7 when spell.Level <= 74 => false,
            <= 3 when spell.Level <= 99 => false,
            _ => true
        };
    }

    private static bool CritStrike()
    {
        var critRoll = Generator.RandNumGen100();
        {
            return critRoll >= 95;
        }
    }

    public void Train(WorldClient client, Spell spell) => client.TrainSpell(spell);

    public long WeaponDamageElementalProc(Sprite sprite, int weaponProc)
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

    public long AislingSpellDamageCalc(Sprite sprite, long baseDmg, Spell spell, double exp)
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

        damageDealingAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, damageDealingAisling.Serial));
        final *= CritDmg;

        return final;
    }

    public long MonsterElementalDamageProc(Sprite sprite, long baseDmg, Spell spell, double exp)
    {
        if (sprite is not Monster damageMonster) return 0;
        var imp = baseDmg + 2.0;
        var level = damageMonster.Level;

        var amp = damageMonster.Int / 2.0 * exp;
        var final = (int)(amp + imp) + level;
        var crit = CritStrike();

        if (!crit) return final;

        damageMonster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, damageMonster.Serial));
        final *= CritDmg;

        return final;
    }

    public void ElementalOnSuccess(Sprite sprite, Sprite target, Spell spell, double exp)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;
            var levelSeed = (long)((aisling.ExpLevel + aisling.AbpLevel) * 0.10 * spell.Level);
            var dmg = AislingSpellDamageCalc(sprite, levelSeed, spell, exp);

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
                target.ApplyElementalSpellDamage(aisling, dmg, spell.Template.ElementalProperty, spell);
            }
            else
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            var dmg = (long)sprite.GetBaseDamage(sprite, sprite.Target, MonsterEnums.Elemental);
            dmg = MonsterElementalDamageProc(sprite, dmg, spell, exp);
            target.ApplyElementalSpellDamage(sprite, dmg, spell.Template.ElementalProperty, spell);

            if (target is Aisling targetAisling)
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} casts {spell.Template.Name} elemental on you");

            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
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
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
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
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
                return;
            }

            SpellOnFailed(sprite, target, spell);
        }
    }

    public void AfflictionOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
    {
        if (target == null) return;

        if (sprite is Aisling aisling)
        {
            if (target.CurrentHp > 0)
            {
                if (target is Aisling targetPlayer)
                    targetPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username} afflicts you with {spell.Template.Name}");

                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
            }

            aisling.Client.EnqueueDebuffAppliedEvent(target, debuff, TimeSpan.FromSeconds(debuff.Length));
        }
        else
        {
            if (target is Aisling targetAisling)
            {
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} afflicts you with {spell.Template.Name}");
                targetAisling.Client.EnqueueDebuffAppliedEvent(target, debuff, TimeSpan.FromSeconds(debuff.Length));
            }
            else
                debuff.OnApplied(target, debuff);

            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public void PoisonOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
    {
        if (target == null) return;
        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") ||
            target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein")) return;

        if (sprite is Aisling aisling)
        {
            if (target.CurrentHp > 0)
            {
                if (target is Aisling targetPlayer)
                    targetPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username} poisons you with {spell.Template.Name}.");

                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.Animation, null, target.Serial));
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.Animation, target.Position));
            }

            aisling.Client.EnqueueDebuffAppliedEvent(target, debuff, TimeSpan.FromSeconds(debuff.Length));
        }
        else
        {
            if (target is Aisling targetAisling)
            {
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} poisoned you with {spell.Template.Name}");
                targetAisling.Client.EnqueueDebuffAppliedEvent(target, debuff, TimeSpan.FromSeconds(debuff.Length));
            }
            else
                debuff.OnApplied(target, debuff);

            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public void SpellOnSuccess(Sprite sprite, Sprite target, Spell spell)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
            }
        }
        else
        {
            if (target is Aisling targetAisling)
                targetAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Unknown"} cast {spell.Template.Name} on you");

            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public void SpellOnFailed(Sprite sprite, Sprite target, Spell spell)
    {
        switch (sprite)
        {
            case Aisling aisling:
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{spell.Template.Name} has failed.");
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial, 50));
                break;
            case Monster:
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial, 50));
                break;
        }
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
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
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
                caster.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
                return;
            }

            SpellOnFailed(sprite, target, spell);
        }
    }

    public void EnhancementOnSuccess(Sprite sprite, Sprite target, Spell spell, Buff buff)
    {
        if (sprite is Aisling aisling)
        {
            if (target == null) return;

            if (target.CurrentHp > 0)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
            }
            else
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
            }

            aisling.Client.EnqueueBuffAppliedEvent(target, buff, TimeSpan.FromSeconds(buff.Length));
        }
        else
        {
            if (target is Aisling targetPlayer)
            {
                if (!target.HasDebuff(buff.Name))
                {
                    targetPlayer.Client.EnqueueBuffAppliedEvent(targetPlayer, buff, TimeSpan.FromSeconds(buff.Length));
                }
            }
            else
            {
                if (!target.HasDebuff(buff.Name))
                    buff.OnApplied(sprite, buff);
            }

            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
    }

    public void EnhancementOnUse(Sprite sprite, Sprite target, Spell spell, Buff buff)
    {
        if (target == null) return;
        if (!spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
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

    public void Step(Sprite sprite, int savedXStep, int savedYStep)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var warpPos = new Position(savedXStep, savedYStep);
        damageDealingSprite.Client.WarpTo(warpPos, true);
        damageDealingSprite.Client.CheckWarpTransitions(damageDealingSprite.Client);
        damageDealingSprite.UpdateAddAndRemove();
        damageDealingSprite.Client.UpdateDisplay();
        damageDealingSprite.Client.LastMovement = DateTime.UtcNow;
    }

    public int DistanceTo(Position spritePos, Position inputPos)
    {
        var spriteX = spritePos.X;
        var spriteY = spritePos.Y;
        var inputX = inputPos.X;
        var inputY = inputPos.Y;
        var diffX = Math.Abs(spriteX - inputX);
        var diffY = Math.Abs(spriteY - inputY);

        return diffX + diffY;
    }

    public void RemoveFakeSnow(Sprite sprite)
    {
        if (sprite.Map.Flags.MapFlagIsSet(MapFlags.Snow))
        {
            Task.Delay(30000).ContinueWith(ct =>
            {
                if (sprite.Map.Flags.MapFlagIsSet(MapFlags.Snow))
                    sprite.Map.Flags &= ~MapFlags.Snow;

                sprite.PlayerNearby?.Client.ClientRefreshed();
            });
        }
    }
}