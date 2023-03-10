using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items;

[Script("Potion")]
public class Potion : ItemScript
{
    public Potion(Item item) : base(item) { }
        
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

                        client.SendMessage(0x02, "Recovered 75% HP");
                        client.Aisling.Animate(364);
                    }
                        break;
                    case "Mor Ioc Deum":
                    {
                        hp = client.Aisling.MaximumHp * .50;
                        client.Aisling.CurrentHp += (int)hp;

                        client.SendMessage(0x02, "Recovered 50% HP");
                        client.Aisling.Animate(363);
                    }
                        break;
                    case "Ioc Deum":
                    {
                        hp = client.Aisling.MaximumHp * .25;
                        client.Aisling.CurrentHp += (int)hp;

                        client.SendMessage(0x02, "Recovered 25% HP");
                        client.Aisling.Animate(168);
                    }
                        break;
                    case "Minor Ao Puinsein Deum":
                    {
                        if (client.Aisling.HasDebuff("Beag Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                        }
                    }
                        break;
                    case "Ao Puinsein Deum":
                    {
                        if (client.Aisling.HasDebuff("Beag Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Mor Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                        }
                    }
                        break;
                    case "Major Ao Puinsein Deum":
                    {
                        if (client.Aisling.HasDebuff("Beag Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Beag Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Mor Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Mor Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
                            break;
                        }

                        if (client.Aisling.HasDebuff("Ard Puinsein"))
                        {
                            client.Aisling.Debuffs.TryRemove("Ard Puinsein", out var debuff);
                            debuff?.OnEnded(client.Aisling, debuff);
                            client.Aisling.Animate(1);
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
                        client.SendMessage(0x02, "Wow, that's delicious! Recovered 25% HP, 10% MP.");
                    }
                        break;
                    case "Pizza Slice":
                    {
                        hp = client.Aisling.MaximumHp * .05;

                        client.Aisling.CurrentHp += (int)hp;
                        client.SendMessage(0x02, "Yum! Recovered 5% HP.");
                    }
                        break;
                    case "Mushroom":
                    {
                        hp = client.Aisling.MaximumHp * .15;
                        mp = client.Aisling.MaximumMp * .10;

                        client.Aisling.CurrentHp += (int)hp;
                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Delicious Fungi! 15% HP, 10% MP.");
                    }
                        break;

                    #endregion

                    #region Alcohol

                    case "Mead":
                    {
                        hp = 50;

                        client.Aisling.CurrentHp -= (int)hp;
                        client.SendMessage(0x02, "That went down smooth. -50 hp");
                    }
                        break;
                    case "Strong Mead":
                    {
                        hp = 150;

                        client.Aisling.CurrentHp -= (int)hp;
                        client.SendMessage(0x02, "Strong! -150 hp");
                    }
                        break;
                    case "Carafe":
                    {
                        hp = 80;
                        mp = 1000;

                        client.Aisling.CurrentHp -= (int)hp;
                        client.Aisling.CurrentMp -= (int)mp;
                        client.SendMessage(0x02, "Too much of a good thing. -80 hp, -1000 mp");
                    }
                        break;
                    case "Wine":
                    {
                        hp = 80;
                        mp = 100;

                        client.Aisling.CurrentHp += (int)hp;
                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "It's good for the heart after all. 80 hp, 100 mp");
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
                        client.SendMessage(0x02, "An apple a day. 50 hp, 100 mp");
                    }
                        break;
                    case "Baguette":
                    {
                        hp = 10;

                        client.Aisling.CurrentHp += (int)hp;
                        client.SendMessage(0x02, "Delicious! 10 hp");
                    }
                        break;
                    case "Beef":
                    {
                        hp = 100;

                        client.Aisling.CurrentHp += (int)hp;
                        client.SendMessage(0x02, "Hearty! 100 hp");
                    }
                        break;

                    case "Carrot":
                    {
                        mp = 100;

                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Nutritious! 100 mp");
                    }
                        break;
                    case "Cherries":
                    {
                        mp = 25;

                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Yum! 25 mp");
                    }
                        break;
                    case "Chicken":
                    {
                        hp = 100;
                        mp = 100;

                        client.Aisling.CurrentHp += (int)hp;
                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Tender! (Tendies ya!) 100 hp, 100 mp");
                    }
                        break;
                    case "Ginger":
                    {
                        hp = client.Aisling.MaximumHp * .05;
                        mp = client.Aisling.MaximumMp * .05;

                        client.Aisling.CurrentHp += (int)hp;
                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Wow, that's powerful! 5% hp, 5% mp");
                    }
                        break;
                    case "Juicy Apple":
                    {
                        hp = client.Aisling.MaximumHp * .02;

                        client.Aisling.CurrentHp += (int)hp;
                        client.SendMessage(0x02, "So healthy! 2% hp");
                    }
                        break;
                    case "Juicy Grapes":
                    {
                        mp = client.Aisling.MaximumMp * .03;

                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "So healthy! 3% mp");
                    }
                        break;
                    case "Leeks":
                    {
                        mp = client.Aisling.MaximumMp * .02;

                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "Getting my veggies on! 2% mp");
                    }
                        break;

                    case "Mold":
                    {
                        hp = client.Aisling.MaximumHp * .07;

                        client.Aisling.CurrentHp += (int)hp;
                        client.SendMessage(0x02, "Almost as good as a doctor! 7% hp");
                    }
                        break;
                    case "Poisonous Tentacle":
                    {
                        hp = client.Aisling.MaximumHp * .33;

                        client.Aisling.CurrentHp -= (int)hp;
                        client.SendMessage(0x02, "I shouldn't eat these. -33% hp");
                    }
                        break;
                    case "Rotten Veggies":
                    {
                        hp = client.Aisling.MaximumHp * .05;

                        client.Aisling.CurrentHp -= (int)hp;
                        client.SendMessage(0x02, "Yea, no good. -5% hp");
                    }
                        break;
                    case "Spoiled Cherries":
                    {
                        mp = client.Aisling.MaximumMp * .08;

                        client.Aisling.CurrentMp -= (int)mp;
                        client.SendMessage(0x02, "I feel my power leaving me. -8% mp");
                    }
                        break;
                    case "Spoiled Grapes":
                    {
                        mp = client.Aisling.MaximumMp * .08;

                        client.Aisling.CurrentMp -= (int)mp;
                        client.SendMessage(0x02, "I feel my power leaving me. -8% mp");
                    }
                        break;
                    case "Tomato":
                    {
                        mp = client.Aisling.MaximumMp * .03;

                        client.Aisling.CurrentMp += (int)mp;
                        client.SendMessage(0x02, "This would be great in a stew. 3% mp");
                    }
                        break;

                    #endregion
                }
                break;
            }
            case AislingFlags.Ghost:
                break;
        }

        client.SendStats(StatusFlags.MultiStat);
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }
}