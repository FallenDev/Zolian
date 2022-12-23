using Darkages.Common;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells
{
    public class GlobalSpellMethods : IGlobalSpellMethods
    {
        private const int CritDmg = 2;

        public bool Execute(IGameClient client, Spell spell)
        {
            if (!client.Aisling.CanCast) return false;
            var success = Generator.RandNumGen100();

            if (spell.Level == 100)
            {
                return success >= 3;
            }

            return success switch
            {
                <= 25 when spell.Level <= 29 => false,
                <= 15 when spell.Level <= 49 => false,
                <= 10 when spell.Level <= 74 => false,
                <= 5 when spell.Level <= 99 => false,
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

        public void Train(IGameClient client, Spell spell)
        {
            var trainPoint = Generator.RandNumGen100();

            switch (trainPoint)
            {
                case <= 5:
                    break;
                case <= 98 and >= 6:
                    client.TrainSpell(spell);
                    break;
                case <= 100 and >= 99:
                    client.TrainSpell(spell);
                    client.TrainSpell(spell);
                    break;
            };
        }

        public long WeaponDamageElementalProc(Sprite sprite, int weaponProc)
        {
            if (sprite is not Aisling damageDealingAisling) return 0;
            var client = damageDealingAisling.Client;
            var level = damageDealingAisling.Level;
            var dmg = client.Aisling.Int * client.Aisling.Str * weaponProc;

            var levelBuff = level switch
            {
                >= 0 and <= 29 => 1,
                >= 30 and <= 49 => 1.2,
                >= 50 and <= 69 => 1.4,
                >= 70 and <= 98 => 1.8,
                >= 99 => 2
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
            var bonus = baseDmg + 2.0 * spellLevelOffset / 100;
            var amp = client.Aisling.Int / 2.0 * exp;
            var final = (int)(amp + bonus);
            var crit = CritStrike();

            if (!crit) return final;

            damageDealingAisling.Animate(387);
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

            sprite.Animate(387);
            final *= CritDmg;

            return final;
        }

        public void ElementalOnSuccess(Sprite sprite, Sprite target, Spell spell, double exp)
        {
            if (sprite is Aisling aisling)
            {
                if (target == null) return;
                var client = aisling.Client;
                var dmg = (long)aisling.GetBaseDamage(aisling, target, MonsterEnums.Elemental);
                dmg = AislingSpellDamageCalc(sprite, dmg, spell, exp);

                aisling.Cast(spell, target);
                target.ApplyElementalSpellDamage(aisling, dmg, spell.Template.ElementalProperty, spell);
                Train(client, spell);
            }
            else
            {
                var dmg = (long)sprite.GetBaseDamage(sprite, sprite.Target, MonsterEnums.Elemental);
                dmg = MonsterElementalDamageProc(sprite, dmg, spell, exp);

                target.ApplyElementalSpellDamage(sprite, dmg, spell.Template.ElementalProperty, spell);

                if (target is Aisling targetAisling)
                    targetAisling.Client
                        .SendMessage(0x02, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Monster"} Attacks you with {spell.Template.Name}.");

                target.SendAnimation(spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                sprite.Show(Scope.NearbyAislings, action);
            }
        }

        public void ElementalOnFailed(Sprite sprite, Sprite target, Spell spell)
        {
            switch (sprite)
            {
                case Aisling aisling:
                    aisling
                        .Client.SendMessage(0x02, $"{spell.Template.Name} has been deflected.");
                    aisling
                        .Client.SendAnimation(115, target, aisling);
                    break;
                case Monster:
                    (sprite.Target as Aisling)?.Client
                        .SendAnimation(115, sprite, target);
                    break;
            }
        }

        public void ElementalOnUse(Sprite sprite, Sprite target, Spell spell, double exp = 1)
        {
            if (!spell.CanUse())
            {
                if (sprite is Aisling)
                    sprite.Client.SendMessage(0x02, "Ability is not quite ready yet.");
                return;
            }

            if (target.SpellReflect)
            {
                target.Animate(184);
                if (sprite is Aisling)
                    sprite.Client.SendMessage(0x02, "Your spell has been reflected!");
                if (target is Aisling)
                    target.Client.SendMessage(0x02, $"You reflected {spell.Template.Name}.");

                sprite = Spell.SpellReflect(target, sprite);
            }

            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                if (aisling.CurrentMp - spell.Template.ManaCost > 0)
                {
                    aisling.CurrentMp -= spell.Template.ManaCost;
                }
                else
                {
                    client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
                    return;
                }

                if (target.SpellNegate)
                {
                    target.Animate(64);
                    client.SendMessage(0x02, "Your spell has been deflected!");
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You deflected {spell.Template.Name}.");

                    return;
                }

                if (aisling.CurrentMp < 0)
                    aisling.CurrentMp = 0;

                var mR = Generator.RandNumGen100();

                if (mR > target.Will)
                {
                    var success = Execute(client, spell);

                    if (success)
                    {
                        if (client.Aisling.Invisible && spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                        {
                            client.Aisling.Invisible = false;
                            client.UpdateDisplay();
                        }

                        ElementalOnSuccess(aisling, target, spell, exp);
                    }
                    else
                    {
                        ElementalOnFailed(aisling, target, spell);
                    }
                }

                client.SendStats(StatusFlags.StructB);
            }
            else
            {
                if (sprite.CurrentMp - spell.Template.ManaCost > 0)
                {
                    sprite.CurrentMp -= spell.Template.ManaCost;
                }

                if (target.SpellReflect)
                {
                    target.Animate(184);
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You reflected {spell.Template.Name}.");

                    sprite = Spell.SpellReflect(target, sprite);
                }

                if (target.SpellNegate)
                {
                    target.Animate(64);
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You deflected {spell.Template.Name}.");

                    return;
                }

                if (sprite.CurrentMp < 0)
                    sprite.CurrentMp = 0;

                var rand = Generator.RandNumGen100();
                {
                    if (rand > target.Will)
                        ElementalOnSuccess(sprite, target, spell, exp);
                    else
                        ElementalOnFailed(sprite, target, spell);
                }
            }
        }

        public void AfflictionOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
        {
            if (sprite is Aisling aisling)
            {
                if (target == null) return;
                var client = aisling.Client;

                aisling.Cast(spell, target);

                if (target.CurrentHp > 0)
                {
                    target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
                }

                debuff.OnApplied(target, debuff);
                Train(client, spell);
            }
            else
            {
                debuff.OnApplied(target, debuff);

                if (target is Aisling targetAisling)
                    targetAisling.Client
                        .SendMessage(0x02, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Monster"} Attacks you with {spell.Template.Name}.");

                target.SendAnimation(spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                sprite.Show(Scope.NearbyAislings, action);
                target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
        }

        public void PoisonOnSuccess(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
        {
            if (target == null) return;
            if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") ||
                target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein")) return;

            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                debuff.OnApplied(target, debuff);

                if (target is Aisling targetPlayer)
                    targetPlayer.Client.SendMessage(0x02, $"{client.Aisling.Username} poisons you with {spell.Template.Name}.");

                client.SendMessage(0x02, $"You've cast {spell.Template.Name}");
                client.SendAnimation(spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = (byte)(client.Aisling.Path switch
                    {
                        Class.Cleric => 0x80,
                        Class.Arcanus => 0x88,
                        _ => 0x06
                    }),
                    Speed = 30
                };

                client.Aisling.Show(Scope.NearbyAislings, action);
                client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
            else
            {
                debuff.OnApplied(target, debuff);

                if (target is Aisling targetAisling)
                    targetAisling.Client
                        .SendMessage(0x02, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Monster"} Attacks you with {spell.Template.Name}.");

                target.SendAnimation(spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                sprite.Show(Scope.NearbyAislings, action);
                target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
        }

        public void SpellOnSuccess(Sprite sprite, Sprite target, Spell spell)
        {
            if (sprite is Aisling aisling)
            {
                if (target == null) return;
                aisling.Cast(spell, target);
                target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
            else
            {
                if (target is Aisling targetAisling)
                    targetAisling.Client
                        .SendMessage(0x02, $"{(sprite is Monster monster ? monster.Template.BaseName : (sprite as Mundane)?.Template.Name) ?? "Monster"} Attacks you with {spell.Template.Name}.");

                target.SendAnimation(spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                sprite.Show(Scope.NearbyAislings, action);
                target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
        }

        public void SpellOnFailed(Sprite sprite, Sprite target, Spell spell)
        {
            switch (sprite)
            {
                case Aisling aisling:
                    aisling.Client.SendMessage(0x02, $"{spell.Template.Name} has failed.");
                    aisling.Client.SendAnimation(115, target ?? aisling, aisling);
                    break;
                case Monster:
                    (sprite.Target as Aisling)?.Client
                        .SendAnimation(115, sprite, target);
                    break;
            }

            if (sprite.Map.Flags.MapFlagIsSet(MapFlags.Snow))
            {
                Task.Delay(30000).ContinueWith(ct =>
                {
                    if (sprite.Map.Flags.MapFlagIsSet(MapFlags.Snow))
                        sprite.Map.Flags &= ~MapFlags.Snow;

                    sprite.Client.ClientRefreshed();
                });
            }
        }

        public void AfflictionOnUse(Sprite sprite, Sprite target, Spell spell, Debuff debuff)
        {
            if (!spell.CanUse())
            {
                if (sprite is Aisling)
                    sprite.Client.SendMessage(0x02, "Ability is not quite ready yet.");
                return;
            };

            if (target.SpellReflect)
            {
                target.Animate(184);
                if (sprite is Aisling)
                    sprite.Client.SendMessage(0x02, "Your spell has been reflected!");
                if (target is Aisling)
                    target.Client.SendMessage(0x02, $"You reflected {spell.Template.Name}.");

                sprite = Spell.SpellReflect(target, sprite);
            }

            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                if (aisling.CurrentMp - spell.Template.ManaCost > 0)
                {
                    aisling.CurrentMp -= spell.Template.ManaCost;
                }
                else
                {
                    client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
                    return;
                }

                if (target.SpellNegate)
                {
                    target.Animate(64);
                    client.SendMessage(0x02, "Your spell has been deflected!");
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You deflected {spell.Template.Name}.");

                    return;
                }

                if (aisling.CurrentMp < 0)
                    aisling.CurrentMp = 0;

                var mR = Generator.RandNumGen100();

                if (mR > target.Will)
                {
                    var success = Execute(client, spell);

                    if (success)
                    {
                        if (client.Aisling.Invisible && spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                        {
                            client.Aisling.Invisible = false;
                            client.UpdateDisplay();
                        }

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
                }

                client.SendStats(StatusFlags.StructB);
            }
            else
            {
                if (sprite.CurrentMp - spell.Template.ManaCost > 0)
                {
                    sprite.CurrentMp -= spell.Template.ManaCost;
                }

                if (target.SpellReflect)
                {
                    target.Animate(184);
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You reflected {spell.Template.Name}.");

                    sprite = Spell.SpellReflect(target, sprite);
                }

                if (target.SpellNegate)
                {
                    target.Animate(64);
                    if (target is Aisling)
                        target.Client.SendMessage(0x02, $"You deflected {spell.Template.Name}.");

                    return;
                }

                if (sprite.CurrentMp < 0)
                    sprite.CurrentMp = 0;

                var rand = Generator.RandNumGen100();
                {
                    if (rand > target.Will)
                    {
                        if (debuff.Name.Contains("Puinsein"))
                        {
                            PoisonOnSuccess(sprite, target, spell, debuff);
                        }
                        else
                        {
                            AfflictionOnSuccess(sprite, target, spell, debuff);
                        }
                    }
                    else
                    {
                        SpellOnFailed(sprite, target, spell);
                    }
                }
            }
        }

        public void EnhancementOnSuccess(Sprite sprite, Sprite target, Spell spell, Buff buff)
        {
            if (sprite is Aisling aisling)
            {
                if (target == null) return;
                var client = aisling.Client;

                aisling.Cast(spell, target);

                if (target.CurrentHp > 0)
                {
                    target.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
                }

                buff.OnApplied(target, buff);
                Train(client, spell);
            }
            else
            {
                buff.OnApplied(sprite, buff);
                sprite.SendAnimation(spell.Template.Animation, sprite, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                sprite.Show(Scope.NearbyAislings, action);
                sprite.Show(Scope.NearbyAislings, new ServerFormat19(spell.Template.Sound));
            }
        }

        public void EnhancementOnUse(Sprite sprite, Sprite target, Spell spell, Buff buff)
        {
            if (!spell.CanUse())
            {
                if (sprite is Aisling)
                    sprite.Client.SendMessage(0x02, "Ability is not quite ready yet.");
                return;
            };

            if (sprite is Aisling aisling)
            {
                var client = aisling.Client;

                if (aisling.CurrentMp - spell.Template.ManaCost > 0)
                {
                    aisling.CurrentMp -= spell.Template.ManaCost;
                }
                else
                {
                    client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
                    return;
                }

                if (aisling.CurrentMp < 0)
                    aisling.CurrentMp = 0;

                var success = Execute(client, spell);

                if (success)
                {
                    if (client.Aisling.Invisible && spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                    {
                        client.Aisling.Invisible = false;
                        client.UpdateDisplay();
                    }

                    EnhancementOnSuccess(sprite, target, spell, buff);
                }
                else
                {
                    SpellOnFailed(aisling, target, spell);
                }


                client.SendStats(StatusFlags.StructB);
            }
            else
            {
                if (sprite.CurrentMp - spell.Template.ManaCost > 0)
                {
                    sprite.CurrentMp -= spell.Template.ManaCost;
                }

                if (sprite.CurrentMp < 0)
                    sprite.CurrentMp = 0;

                EnhancementOnSuccess(sprite, target, spell, buff);
            }
        }

        public void Step(Sprite sprite, int savedXStep, int savedYStep)
        {
            if (sprite is not Aisling damageDealingSprite) return;
            var warpPos = new Position(savedXStep, savedYStep);
            damageDealingSprite.Client.WarpTo(warpPos, true);
            GameServer.CheckWarpTransitions(damageDealingSprite.Client);
            damageDealingSprite.UpdateAddAndRemove();
            damageDealingSprite.Client.UpdateDisplay();
            damageDealingSprite.Client.LastMovement = DateTime.Now;
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
    }
}
