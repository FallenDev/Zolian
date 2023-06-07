using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Item Shop")]
public class ItemShop : MundaneScript
{
    private long _repairSum;

    public ItemShop(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var opts = new List<OptionsDataItem>
        {
            new (0x0001, "Buy"),
            new (0x0002, "Sell"),
            new (0x0003, "Repair All Items")
        };

        client.SendOptionsDialog(Mundane, "Take a look, we're always in stock.", opts.ToArray());
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x0001:
                client.SendItemShopDialog(Mundane, "Here's what I have to offer.", 0x0004, ShopMethods.BuyFromStoreInventory(Mundane));
                break;
            case 0x0002:
                client.SendItemSellDialog(Mundane, "What do you want to sell?", 0x0005, ShopMethods.GetCharacterSellInventoryByteList(client));
                break;
            case 0x0030:
            {
                if (client.PendingItemSessions != null)
                {
                    ShopMethods.CompletePendingItemSell(client);
                }

                TopMenu(client);
            }
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
                        var opts = new List<OptionsDataItem>
                        {
                            new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                            new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                        };

                        client.SendOptionsDialog(Mundane, $"It will cost you a total of {cost} coins for {{=c{amount} {{=q{item}s{{=a. Is that a deal?", opts.ToArray());
                    }
                }

                if (amount > 0 && client.PendingItemSessions != null)
                {
                    client.PendingItemSessions.Quantity = amount;

                    var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

                    if (item != null)
                    {
                        var offer = (int)(item.Template.Value / 2);

                        if (item.Stacks >= amount)
                        {
                            if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                            {
                                client.PendingItemSessions.Offer = (uint)(offer * amount);
                                client.PendingItemSessions.Removing = amount;

                                var opts2 = new List<OptionsDataItem>
                                {
                                    new(0x0030, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                    new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                };

                                client.SendOptionsDialog(Mundane, string.Format("I can offer you {0} gold for {{=c{1} {{=q{3}s {{=a({2} Gold Each), is that a deal?",
                                    client.PendingItemSessions.Offer, amount, client.PendingItemSessions.Offer / amount, item.Template.Name), opts2.ToArray());
                            }
                            else
                            {
                                client.PendingItemSessions = null;
                                client.SendOptionsDialog(Mundane, "Looks like you can't carry anymore gold. Why don't you buy something?");
                            }
                        }
                        else
                        {
                            client.PendingItemSessions = null;
                            client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantStackErrorMessage);
                        }
                    }
                }
            }
                break;
            case 0x0500:
            {
                var itemFromSlot = client.Aisling.Inventory.Get(i => i != null).ToList().Find(i => i.InventorySlot == Convert.ToInt32(args));

                if (itemFromSlot != null)
                {
                    var offer = (int)(itemFromSlot.Template.Value / 2);

                    if (offer <= 0)
                    {
                        client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantRefuseTradeMessage);
                        return;
                    }

                    if (itemFromSlot.Stacks > 1 && itemFromSlot.Template.CanStack)
                    {
                        client.PendingItemSessions = new PendingSell
                        {
                            ID = itemFromSlot.ItemId,
                            Name = itemFromSlot.Template.Name,
                            Quantity = 0
                        };

                        client.Send(new ServerFormat2F(Mundane,
                            $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}",
                            new TextInputData()));
                    }
                    else
                    {
                        client.PendingItemSessions = new PendingSell
                        {
                            ID = itemFromSlot.ItemId,
                            Name = itemFromSlot.Template.Name,
                            Quantity = 1
                        };

                        var opts2 = new List<OptionsDataItem>
                        {
                            new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                            new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                        };

                        client.SendOptionsDialog(Mundane,
                            $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)",
                            itemFromSlot.Template.Name, opts2.ToArray());
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
                    var cost = (uint)(client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity);
                    if (client.Aisling.GoldPoints >= cost)
                    {
                        client.Aisling.GoldPoints -= cost;
                        client.GiveQuantity(client.Aisling, item, quantity);
                        client.SendStats(StatusFlags.WeightMoney);
                        client.PendingBuySessions = null;
                        client.SendMessage(0x03, $"{{=cThank you!");
                        TopMenu(client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Well? Where is it?");
                        client.PendingBuySessions = null;
                    }
                }

                if (client.PendingItemSessions != null)
                {
                    var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

                    if (item == null) return;

                    var offer = item.Template.Value / 2;

                    if (offer <= 0) return;
                    if (offer > item.Template.Value) return;

                    if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                    {
                        client.Aisling.GoldPoints += offer;
                        client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                        client.SendStats(StatusFlags.WeightMoney);
                        client.PendingItemSessions = null;
                        client.SendMessage(0x03, $"{{=cThank you!");
                        TopMenu(client);
                    }
                }
            }
                break;
            #region Buy
            case 0x0020:
            {
                client.PendingItemSessions = null;
                client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
            }
                break;
            case 0x0004:
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
                        item = item.Create(client.Aisling, template, ShopMethods.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());

                        if (item.GiveTo(client.Aisling))
                        {
                            client.Aisling.GoldPoints -= template.Value;

                            client.SendStats(StatusFlags.WeightMoney);
                            client.SendOptionsDialog(Mundane, $"You've purchased: {{=c{args}");
                        }
                        else
                        {
                            client.SendMessage(0x02, "Yeah right, You can't even physically hold it.");
                        }

                        break;
                    }
                    case false:
                    {
                        client.SendOptionsDialog(Mundane, "Put that back or I'll turn you into a toad!");
                        break;
                    }
                }
            }
                break;
            #endregion
            #region Repair
            case 0x0003:
            {
                _repairSum = ShopMethods.GetRepairCosts(client);

                var optsRepair = new List<OptionsDataItem>
                {
                    new(0x0014, ServerSetup.Instance.Config.MerchantConfirmMessage),
                    new(0x0015, ServerSetup.Instance.Config.MerchantCancelMessage)
                };

                if (_repairSum == 0)
                {
                    client.SendOptionsDialog(Mundane, "Your items are in good condition, no repairs are necessary.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane,
                        "It will cost {=c" + _repairSum + "{=a Gold to repair everything. Do you Agree?",
                        _repairSum.ToString(), optsRepair.ToArray());
                }
            }
                break;
            case 0x0014:
            {
                if (client.Aisling.GoldPoints >= Convert.ToUInt32(_repairSum))
                {
                    client.Aisling.GoldPoints -= Convert.ToUInt32(_repairSum);

                    client.RepairEquipment();
                    client.SendOptionsDialog(Mundane, "Just finished, let me know how it turned out.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "Well? Where is it?");
                }
            }
                break;
            case 0x0015:
                client.SendOptionsDialog(Mundane, "Come back before anything breaks.");
                break;
            #endregion
        }
    }

    public override void OnItemDropped(GameClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
        {
            OnResponse(client, 0x0500, item.InventorySlot.ToString());
        }
    }
}