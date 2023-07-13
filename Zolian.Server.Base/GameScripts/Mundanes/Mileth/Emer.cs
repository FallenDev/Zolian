using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Emer")]
public class Emer : MundaneScript
{
    public Emer(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new (0x01, "Buy")
        };

        switch (client.Aisling.QuestManager.KillerBee)
        {
            case false:
            {
                if (client.Aisling.Level >= 11 || client.Aisling.GameMaster)
                {
                    options.Add(new Dialog.OptionsDataItem(0x03, "Killer Bee Jam"));
                }

                break;
            }
            case true:
                options.Add(new Dialog.OptionsDataItem(0x06, "Here's the corpse"));
                break;
        }

        client.SendOptionsDialog(Mundane, "Are ye hungry?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            #region Buy
            case 0x01:
                client.SendItemShopDialog(Mundane, "Bon Appetite.", 0x02, ShopMethods.BuyFromStoreInventory(Mundane));
                break;
            case 0x0000:
            {
                if (string.IsNullOrEmpty(args)) return;

                int.TryParse(args, out var amount);
                if (amount > 0 && client.PendingBuySessions != null)
                {
                    client.PendingBuySessions.Quantity = amount;
                    var item = client.PendingBuySessions.Name;
                    if (item != null)
                    {
                        var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                        var opts = new List<Dialog.OptionsDataItem>
                        {
                            new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                            new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                        };
                        client.SendOptionsDialog(Mundane, $"It will cost you a total of {cost} coins for {{=c{amount} {{=q{item}s{{=a. Is that a deal?", opts.ToArray());
                    }
                }
            }
                break;
            case 0x0019:
            {
                if (client.PendingBuySessions != null)
                {
                    var quantity = client.PendingBuySessions.Quantity;
                    var item = client.PendingBuySessions.Name;
                    var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                    if (client.Aisling.GoldPoints >= cost)
                    {
                        client.Aisling.GoldPoints -= Convert.ToUInt32(cost);
                        client.GiveQuantity(client.Aisling, item, quantity);
                        client.SendAttributes(StatUpdateType.Full);
                        client.PendingBuySessions = null;
                        client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantTradeCompletedMessage);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Well? Where is it?");
                        client.PendingBuySessions = null;
                    }
                }
                else
                {
                    var v = args;
                    var item = client.Aisling.Inventory.Get(i => i != null && i.Template.Name == v)
                        .FirstOrDefault();

                    if (item == null)
                        return;

                    var offer = Convert.ToString((int)(item.Template.Value / 2));

                    if (Convert.ToUInt32(offer) <= 0)
                        return;

                    if (Convert.ToUInt32(offer) > item.Template.Value)
                        return;

                    if (client.Aisling.GoldPoints + Convert.ToUInt32(offer) <=
                        ServerSetup.Instance.Config.MaxCarryGold)
                    {
                        client.Aisling.GoldPoints += Convert.ToUInt32(offer);
                        client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                        client.SendAttributes(StatUpdateType.WeightGold);

                        client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantTradeCompletedMessage);
                    }
                }
            }
                break;
            case 0x0020:
            {
                client.PendingItemSessions = null;
                client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
            }
                break;
            case 0x0002:
            {
                if (string.IsNullOrEmpty(args)) return;
                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(args)) return;

                var template = ServerSetup.Instance.GlobalItemTemplateCache[args];
                switch (template.CanStack)
                {
                    case true:
                        client.PendingBuySessions = new PendingBuy
                        {
                            Name = template.Name,
                            Quantity = 0,
                            Offer = (int)template.Value,
                        };
                        client.Send(new ServerFormat2F(Mundane, $"How many {template.Name} would you like to purchase?", new TextInputData()));
                        break;
                    case false when client.Aisling.GoldPoints >= template.Value:
                    {
                        var item = new Item();
                        item = item.Create(client.Aisling, template, Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

                        if (item.GiveTo(client.Aisling))
                        {
                            client.Aisling.GoldPoints -= template.Value;

                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane, $"You've purchased: {{=c{args}");
                        }
                        else
                        {
                            client.SendServerMessage(ServerMessageType.OrangeBar1, "Yeah right, You can't even physically hold it.");
                        }

                        break;
                    }
                    case false:
                    {
                        if (ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey("Beag Cradh"))
                        {
                            var scripts = ScriptManager.Load<SpellScript>("Beag Cradh",
                                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache["Beag Cradh"]));
                            {
                                foreach (var script in scripts.Values)
                                    script.OnUse(Mundane, client.Aisling);
                            }
                            client.SendOptionsDialog(Mundane, "Are you trying to rip me off?!");
                        }

                        break;
                    }
                }
            }

                break;
            #endregion
            case 0x03:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x04, "I'll take care of them."),
                    new (0x05, "Sorry, I'm busy.")
                };

                client.SendOptionsDialog(Mundane, $"Killer bees in the Eastern Woodlands have been causing our town some issues. Please go to \n{{=qPath 2 {{=a and bring me back a {{=vKiller Bee's Corpse{{=a.", options.ToArray());
            }
                break;
            case 0x04:
            {
                client.SendOptionsDialog(Mundane, "Thank you, it'll sure make some wicked jam.");
                client.Aisling.QuestManager.KillerBee = true;
                break;
            }
            case 0x05:
            {
                client.SendOptionsDialog(Mundane, "Well someone has to do something..");
                break;
            }
            case 0x06:
            {
                if (client.Aisling.HasItem("Killer Bee Corpse"))
                {
                    client.Aisling.QuestManager.KillerBee = false;
                    client.TakeAwayQuantity(client.Aisling, "Killer Bee Corpse", 1);
                    client.GiveExp(2500);
                    var item = new Item();
                    item = item.Create(client.Aisling, "Honey Bacon Burger", Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
                    item.GiveTo(client.Aisling);
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 2,500 experience.");
                    client.SendAttributes(StatUpdateType.WeightGold);
                    client.SendOptionsDialog(Mundane, "Lovely, I'll take as many as you have. Here's a taste of what I make from it.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, $"Have you killed any yet? Bring back proof.");
                }

                break;
            }
        }
    }
}