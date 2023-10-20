using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;

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
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(364, null, client.Aisling.Serial));
                            }
                            break;
                        case "Mor Ioc Deum":
                            {
                                hp = client.Aisling.MaximumHp * .50;
                                client.Aisling.CurrentHp += (int)hp;

                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered 50% HP");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(363, null, client.Aisling.Serial));
                            }
                            break;
                        case "Ioc Deum":
                            {
                                hp = client.Aisling.MaximumHp * .25;
                                client.Aisling.CurrentHp += (int)hp;

                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered 25% HP");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(168, null, client.Aisling.Serial));
                            }
                            break;
                        case "Orcish Strength":
                            {
                                var buff = new buff_OrcishStrength();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff, TimeSpan.FromSeconds(buff.Length));
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your muscles harden (+50 STR)");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(34, null, client.Aisling.Serial));
                            }
                            break;
                        case "Gryphon's Grace":
                            {
                                var buff = new buff_GryphonsGrace();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff, TimeSpan.FromSeconds(buff.Length));
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel lighter on your feet (+50 DEX)");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(86, null, client.Aisling.Serial));
                            }
                            break;
                        case "Feywild Nectar":
                            {
                                var buff = new buff_FeywildNectar();
                                client.EnqueueBuffAppliedEvent(client.Aisling, buff, TimeSpan.FromSeconds(buff.Length));
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Feys dance around you in delight (+50 INT & WIS");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(35, null, client.Aisling.Serial));
                            }
                            break;
                        case "Draconic Vitality":
                            {
                                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Recovered Fully");
                                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(46, null, client.Aisling.Serial));
                            }
                            break;
                        case "Minor Ao Puinsein Deum":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                }
                            }
                            break;
                        case "Ao Puinsein Deum":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Mor Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                }
                            }
                            break;
                        case "Major Ao Puinsein Deum":
                            {
                                if (client.Aisling.HasDebuff("Beag Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Mor Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                    break;
                                }

                                if (client.Aisling.HasDebuff("Ard Puinsein"))
                                {
                                    client.Aisling.Debuffs.TryRemove("Ard Puinsein", out var debuff);
                                    debuff?.OnEnded(client.Aisling, debuff);
                                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, null, client.Aisling.Serial));
                                }
                            }
                            break;

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

                        #endregion

                        #region Alcohol

                        case "Mead":
                            {
                                hp = 50;

                                client.Aisling.CurrentHp -= (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "That went down smooth. -50 hp");
                            }
                            break;
                        case "Strong Mead":
                            {
                                hp = 150;

                                client.Aisling.CurrentHp -= (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Strong! -150 hp");
                            }
                            break;
                        case "Carafe":
                            {
                                hp = 80;
                                mp = 1000;

                                client.Aisling.CurrentHp -= (int)hp;
                                client.Aisling.CurrentMp -= (int)mp;
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
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I shouldn't eat these. -33% hp");
                            }
                            break;
                        case "Rotten Veggies":
                            {
                                hp = client.Aisling.MaximumHp * .05;

                                client.Aisling.CurrentHp -= (int)hp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "Yea, no good. -5% hp");
                            }
                            break;
                        case "Spoiled Cherries":
                            {
                                mp = client.Aisling.MaximumMp * .08;

                                client.Aisling.CurrentMp -= (int)mp;
                                client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel my power leaving me. -8% mp");
                            }
                            break;
                        case "Spoiled Grapes":
                            {
                                mp = client.Aisling.MaximumMp * .08;

                                client.Aisling.CurrentMp -= (int)mp;
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