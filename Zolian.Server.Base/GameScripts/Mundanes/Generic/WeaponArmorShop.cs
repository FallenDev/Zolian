using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Weapon Armor Shop")]
public class WeaponArmorShop : MundaneScript
{
    private long _repairSum;

    public WeaponArmorShop(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>
        {
            new (0x0001, "Buy"),
            new (0x0002, "Sell"),
            new (0x0003, "Repair All Items")
        };

        client.SendOptionsDialog(Mundane, "What can I forge for you?", opts.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x0001:
                client.SendItemShopDialog(Mundane, "This is what I have on the racks.", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x0002:
                client.SendItemSellDialog(Mundane, "What do you want to sell?", NpcShopExtensions.GetCharacterSellInventoryByteList(client));
                break;
            case 0x0030:
                {
                    if (client.PendingItemSessions != null)
                    {
                        NpcShopExtensions.CompletePendingItemSell(client, Mundane);
                    }

                    TopMenu(client);
                }
                break;
            case 0x0000:
                {
                    if (string.IsNullOrEmpty(args)) return;

                    var itemOrSlot = ushort.TryParse(args, out var amountOrSlot);

                    switch (itemOrSlot)
                    {
                        case false:
                            {
                                // ToDo: Add in reactor to buy multiple if a stackable item
                                amountOrSlot = 1;

                                ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(args, out var itemTemplate);
                                if (itemTemplate == null)
                                {
                                    client.SendOptionsDialog(Mundane, "I'm sorry, freshly sold out.");
                                    return;
                                }

                                client.PendingBuySessions = new PendingBuy
                                {
                                    Name = itemTemplate.Name,
                                    Offer = (int)itemTemplate.Value,
                                    Quantity = 1
                                };

                                var itemName = client.PendingBuySessions.Name;

                                if (itemName != null)
                                {
                                    var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                                    var opts = new List<Dialog.OptionsDataItem>
                                    {
                                        new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                        new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                    };

                                    client.SendOptionsDialog(Mundane, $"It will cost you a total of {cost} coins for {{=c{amountOrSlot} {{=q{itemName}s{{=a. Is that a deal?", opts.ToArray());
                                }

                                break;
                            }
                        case true when amountOrSlot > 0:
                            {
                                client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var itemFromSlot);

                                if (itemFromSlot == null)
                                {
                                    client.SendOptionsDialog(Mundane, "Sorry, what was that again?");
                                    return;
                                }

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

                                    client.SendTextInput(Mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}");
                                }
                                else
                                {
                                    client.PendingItemSessions = new PendingSell
                                    {
                                        ID = itemFromSlot.ItemId,
                                        Name = itemFromSlot.Template.Name,
                                        Quantity = 1
                                    };

                                    var opts2 = new List<Dialog.OptionsDataItem>
                                    {
                                        new(0x0030, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                        new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                    };

                                    client.SendOptionsDialog(Mundane,
                                        $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)",
                                        itemFromSlot.Template.Name, opts2.ToArray());
                                }

                                break;
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

                            client.SendTextInput(Mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}");
                        }
                        else
                        {
                            client.PendingItemSessions = new PendingSell
                            {
                                ID = itemFromSlot.ItemId,
                                Name = itemFromSlot.Template.Name,
                                Quantity = 1
                            };

                            var opts2 = new List<Dialog.OptionsDataItem>
                        {
                            new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                            new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                        };

                            client.SendOptionsDialog(Mundane, $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)", itemFromSlot.Template.Name, opts2.ToArray());
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
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.PendingBuySessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
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
                            client.Aisling.Inventory.RemoveFromInventory(client, item);
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.PendingItemSessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
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
                                Offer = (int)template.Value
                            };
                            client.SendTextInput(Mundane, $"How many {template.Name} would you like to purchase?");
                            break;
                        case false when client.Aisling.GoldPoints >= template.Value:
                            {
                                var item = new Item();
                                item = item.Create(client.Aisling, template, NpcShopExtensions.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());

                                if (item.GiveTo(client.Aisling))
                                {
                                    client.Aisling.GoldPoints -= template.Value;

                                    client.SendAttributes(StatUpdateType.WeightGold);
                                    client.SendOptionsDialog(Mundane, $"You've purchased: {{=c{args}");
                                }
                                else
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Yeah right, you can't even physically hold it.");
                                }

                                break;
                            }
                        case false:
                            {
                                client.SendOptionsDialog(Mundane, "You don't favor your limbs, do ya?!");
                                break;
                            }
                    }
                }
                break;
            #endregion
            #region Repair
            case 0x0003:
                {
                    _repairSum = NpcShopExtensions.GetRepairCosts(client);

                    var optsRepair = new List<Dialog.OptionsDataItem>
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

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
        {
            OnResponse(client, 0x0500, item.InventorySlot.ToString());
        }
    }
}