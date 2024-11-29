using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Items;

[Script("Potion")]
public class Potion(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        switch (client.Aisling.Flags)
        {
            case AislingFlags.Normal:
                {
                    double hp;
                    double mp;

                    switch (Item.Template.Name)
                    {
                        #region Potions

                        case "Elixir of Life":
                            {
                                aisling.ReviveInFront();
                            }
                            break;
                        case "Ard Ioc Deum":
                            {
                                hp = client.Aisling.MaximumHp * .75;
                                client.Aisling.CurrentHp += (int)hp;

                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered 75% HP");
                                client.Aisling.SendAnimationNearby(364, null, client.Aisling.Serial);
                            }
                            break;
                        case "Mor Ioc Deum":
                            {
                                hp = client.Aisling.MaximumHp * .50;
                                client.Aisling.CurrentHp += (int)hp;

                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered 50% HP");
                                client.Aisling.SendAnimationNearby(363, null, client.Aisling.Serial);
                            }
                            break;
                        case "Ioc Deum":
                            {
                                hp = client.Aisling.MaximumHp * .25;
                                client.Aisling.CurrentHp += (int)hp;

                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered 25% HP");
                                client.Aisling.SendAnimationNearby(168, null, client.Aisling.Serial);
                            }
                            break;
                        case "Orcish Strength":
                            {
                                var buff = new buff_OrcishStrength();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff);
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your muscles harden (+50 STR)");
                                client.Aisling.SendAnimationNearby(34, null, client.Aisling.Serial);
                            }
                            break;
                        case "Gryphon's Grace":
                            {
                                var buff = new buff_GryphonsGrace();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff);
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel lighter on your feet (+50 DEX)");
                                client.Aisling.SendAnimationNearby(86, null, client.Aisling.Serial);
                            }
                            break;
                        case "Feywild Nectar":
                            {
                                var buff = new buff_FeywildNectar();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff);
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Feys dance around you in delight (+50 INT & WIS");
                                client.Aisling.SendAnimationNearby(35, null, client.Aisling.Serial);
                            }
                            break;
                        case "Draconic Vitality":
                            {
                                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered Fully");
                                client.Aisling.SendAnimationNearby(46, null, client.Aisling.Serial);
                            }
                            break;
                        case "Elemental Essence":
                            {
                                if (client.Aisling.HasBuff("Ard Fas Nadur"))
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
                                    return;
                                }

                                if (client.Aisling.HasBuff("Mor Fas Nadur"))
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
                                    return;
                                }

                                if (client.Aisling.HasBuff("Fas Nadur") || client.Aisling.HasBuff("Beag Fas Nadur"))
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
                                    return;
                                }

                                var buff = new BuffMorFasNadur();
                                buff.OnApplied(client.Aisling, buff);
                                client.Aisling.SendAnimationNearby(67, null, client.Aisling.Serial);
                                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(20, false));
                            }
                            break;
                        case "Minor Ao Puinsein Deum":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                }
                            }
                            break;
                        case "Ao Puinsein Deum":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Mor Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                }
                            }
                            break;
                        case "Antidote":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Mor Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Ard Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Ard Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                }
                            }
                            break;
                        case "Eyedrops":
                            {
                                if (client.Aisling.HasDebuff("Blind"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Blind", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                                }

                                break;
                            }

                        #endregion

                        #region Cooked Food

                        case "Honey Bacon Burger":
                            {
                                hp = client.Aisling.MaximumHp * .25;
                                mp = client.Aisling.MaximumMp * .10;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow, that's delicious! Recovered 25% HP, 10% MP.");
                            }
                            break;
                        case "Pizza Slice":
                            {
                                hp = client.Aisling.MaximumHp * .05;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Yum! Recovered 5% HP.");
                            }
                            break;
                        case "Mushroom":
                            {
                                hp = client.Aisling.MaximumHp * .15;
                                mp = client.Aisling.MaximumMp * .10;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Delicious Fungi! 15% HP, 10% MP.");
                            }
                            break;
                        case "Whole Holiday Turkey":
                            {
                                aisling.ReviveInFront();
                                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "A cure all for holiday blues!  Revive, Full Hp/Mp, Dia Aite");
                                var buff = new buff_DiaAite();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff);
                            }
                            break;
                        case "Hamper of Treats":
                            {
                                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I'm so stuffed!  Full Hp/Mp & Ard Fas Nadur");

                                if (client.Aisling.HasBuff("Ard Fas Nadur"))
                                {
                                    return;
                                }

                                if (client.Aisling.HasBuff("Mor Fas Nadur"))
                                {
                                    return;
                                }

                                if (client.Aisling.HasBuff("Fas Nadur") || client.Aisling.HasBuff("Beag Fas Nadur"))
                                {
                                    return;
                                }
                                
                                var buff = new BuffArdFasNadur();
                                buff.OnApplied(client.Aisling, buff);
                                client.Aisling.SendAnimationNearby(67, null, client.Aisling.Serial);
                                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(20, false));
                            }
                            break;

                        #endregion

                        #region Alcohol

                        case "Mead":
                            {
                                hp = 50;

                                client.Aisling.CurrentHp -= (int)hp;
                                if (client.Aisling.CurrentHp <= 0)
                                    client.Aisling.CurrentHp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "That went down smooth. -50 hp");
                            }
                            break;
                        case "Strong Mead":
                            {
                                hp = 150;

                                client.Aisling.CurrentHp -= (int)hp;
                                if (client.Aisling.CurrentHp <= 0)
                                    client.Aisling.CurrentHp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Strong! -150 hp");
                            }
                            break;
                        case "Carafe":
                            {
                                hp = 80;
                                mp = 1000;

                                client.Aisling.CurrentHp -= (int)hp;
                                if (client.Aisling.CurrentHp <= 0)
                                    client.Aisling.CurrentHp = 1;
                                client.Aisling.CurrentMp -= (int)mp;
                                if (client.Aisling.CurrentMp <= 0)
                                    client.Aisling.CurrentMp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Too much of a good thing. -80 hp, -1000 mp");
                            }
                            break;
                        case "Wine":
                            {
                                hp = 80;
                                mp = 100;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "It's good for the heart after all. 80 hp, 100 mp");
                            }
                            break;
                        case "Rum":
                            {
                                hp = 500;
                                mp = 180;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp -= (int)mp;
                                if (client.Aisling.CurrentMp <= 0)
                                    client.Aisling.CurrentMp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Rummmm, why.. is all the rum Gone! 500 hp, -180 mp");

                                var rand = Generator.RandomNumPercentGen();
                                if (rand >= 0.99)
                                {
                                    var buff = new buff_drunkenFist();
                                    buff.OnApplied(aisling, buff);
                                }
                            }
                            break;
                        case "Holiday Sake":
                            {
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "So delicious! Hic..!  Ao Sith");

                                foreach (var debuff in aisling.Debuffs.Values)
                                {
                                    if (debuff.Affliction) continue;
                                    if (debuff.Name is "Skulled") continue;
                                    debuff.OnEnded(aisling, debuff);
                                }

                                foreach (var buff in aisling.Buffs.Values)
                                {
                                    if (buff.Affliction) continue;
                                    if (buff.Name is "Double XP" or "Triple XP" or "Dia Haste") continue;
                                    buff.OnEnded(aisling, buff);
                                }
                            }
                            break;

                        #endregion

                        #region Basic Food Regions

                        case "Apple":
                            {
                                hp = 50;
                                mp = 100;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "An apple a day. 50 hp, 100 mp");
                            }
                            break;
                        case "Baguette":
                            {
                                hp = 10;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Delicious! 10 hp");
                            }
                            break;
                        case "Beef":
                            {
                                hp = 100;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Hearty! 100 hp");
                            }
                            break;

                        case "Carrot":
                            {
                                mp = 100;

                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Nutritious! 100 mp");
                            }
                            break;
                        case "Cherries":
                            {
                                mp = 25;

                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Yum! 25 mp");
                            }
                            break;
                        case "Chicken":
                            {
                                hp = 100;
                                mp = 100;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Tender! (Tendies ya!) 100 hp, 100 mp");
                            }
                            break;
                        case "Ginger":
                            {
                                hp = client.Aisling.MaximumHp * .05;
                                mp = client.Aisling.MaximumMp * .05;

                                client.Aisling.CurrentHp += (int)hp;
                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow, that's powerful! 5% hp, 5% mp");
                            }
                            break;
                        case "Juicy Apple":
                            {
                                hp = client.Aisling.MaximumHp * .02;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "So healthy! 2% hp");
                            }
                            break;
                        case "Juicy Grapes":
                            {
                                mp = client.Aisling.MaximumMp * .03;

                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "So healthy! 3% mp");
                            }
                            break;
                        case "Leeks":
                            {
                                mp = client.Aisling.MaximumMp * .02;

                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Getting my veggies on! 2% mp");
                            }
                            break;

                        case "Mold":
                            {
                                hp = client.Aisling.MaximumHp * .07;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Almost as good as a doctor! 7% hp");
                            }
                            break;
                        case "Poisonous Tentacle":
                            {
                                hp = client.Aisling.MaximumHp * .33;

                                client.Aisling.CurrentHp -= (int)hp;
                                if (client.Aisling.CurrentHp <= 0)
                                    client.Aisling.CurrentHp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I shouldn't eat these. -33% hp");
                            }
                            break;
                        case "Red Tentacle":
                            {
                                hp = client.Aisling.MaximumHp * .35;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow, that's really fresh! 35% hp");
                            }
                            break;
                        case "Kraken Tentacle":
                            {
                                hp = client.Aisling.MaximumHp * .40;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow, that's really fresh! 40% hp");
                            }
                            break;
                        case "Rotten Veggies":
                            {
                                hp = client.Aisling.MaximumHp * .05;

                                client.Aisling.CurrentHp -= (int)hp;
                                if (client.Aisling.CurrentHp <= 0)
                                    client.Aisling.CurrentHp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Yea, no good. -5% hp");
                            }
                            break;
                        case "Spoiled Cherries":
                            {
                                mp = client.Aisling.MaximumMp * .08;

                                client.Aisling.CurrentMp -= (int)mp;
                                if (client.Aisling.CurrentMp <= 0)
                                    client.Aisling.CurrentMp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel my power leaving me. -8% mp");
                            }
                            break;
                        case "Spoiled Grapes":
                            {
                                mp = client.Aisling.MaximumMp * .08;

                                client.Aisling.CurrentMp -= (int)mp;
                                if (client.Aisling.CurrentMp <= 0)
                                    client.Aisling.CurrentMp = 1;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel my power leaving me. -8% mp");
                            }
                            break;
                        case "Tomato":
                            {
                                mp = client.Aisling.MaximumMp * .03;

                                client.Aisling.CurrentMp += (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "This would be great in a stew. 3% mp");
                            }
                            break;
                        case "Fresh Cod":
                            {
                                hp = client.Aisling.MaximumHp * .25;

                                client.Aisling.CurrentHp += (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow, that's really fresh! 25% hp");
                            }
                            break;
                        case "Fresh Blowfish":
                            {
                                var rand = Generator.RandomNumPercentGen();
                                if (rand >= 0.50)
                                {
                                    client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "A fish with a bite! Yum! 100% hp");
                                    return;
                                }

                                client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bA fish with a bite! Ouch!");
                                var debuff = new DebuffReaping();
                                client.EnqueueDebuffAppliedEvent(client.Aisling, debuff);
                            }
                            break;
                            #endregion
                    }
                    break;
                }
            case AislingFlags.Ghost:
                break;
        }

        client.SendAttributes(StatUpdateType.Full);
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }
}