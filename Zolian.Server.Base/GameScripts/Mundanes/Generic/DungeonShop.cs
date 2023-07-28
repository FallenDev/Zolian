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

[Script("Dungeon Shop")]
public class DungeonShop : MundaneScript
{
    public DungeonShop(WorldServer server, Mundane mundane) : base(server, mundane) { }

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
            //new (0x01, "Dungeon Rumors"),
            new (0x02, "Buy"),
            new (0x03, "Pawn")
        };

        client.SendOptionsDialog(Mundane, "Greetings Adventurer, look no further.\nI have exactly what you're looking for.\nAll items purchased have a quality range of (Common => Rare)", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
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
            case 0x02:
                {
                    client.SendItemShopDialog(Mundane, "Only the finest of gear is sold here.", 0x04, ShopMethods.BuyFromStoreInventory(Mundane));
                }
                break;
            case 0x03:
                {
                    client.SendItemSellDialog(Mundane, "What do you want to pawn?", 0x0005, ShopMethods.GetCharacterSellInventoryByteList(client));
                }
                break;
            case 0x04:
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
                            client.SendTextInput(Mundane, $"How many {template.Name} would you like to purchase?");
                            break;
                        case false when client.Aisling.GoldPoints >= template.Value:
                            {
                                var item = new Item();
                                item = item.Create(client.Aisling, template, ShopMethods.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());

                                if (item.GiveTo(client.Aisling))
                                {
                                    client.Aisling.GoldPoints -= template.Value;

                                    client.SendAttributes(StatUpdateType.WeightGold);
                                    client.SendOptionsDialog(Mundane, $"Ah, one of our best items.\n{{=c{args}{{=a, it will serve you well.");
                                }
                                else
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Yeah right, you can't even physically hold it.");
                                }
                                break;
                            }
                        case false:
                            {
                                if (ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey("Ard Cradh"))
                                {
                                    var scripts = ScriptManager.Load<SpellScript>("Ard Cradh",
                                        Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache["Ard Cradh"]));
                                    {
                                        foreach (var script in scripts.Values)
                                            script.OnUse(Mundane, client.Aisling);
                                    }
                                    client.SendOptionsDialog(Mundane, "You think I'm a fool?! Come back when you have the gold.");
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
                        var offer = (int)(itemFromSlot.Template.Value / 2.2);

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

                            client.SendOptionsDialog(Mundane,
                                $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2.2x)",
                                itemFromSlot.Template.Name, opts2.ToArray());
                        }
                    }
                }
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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cHope it serves you well.");
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

                        var offer = (uint)(item.Template.Value / 2.4);

                        if (offer <= 0) return;
                        if (offer > item.Template.Value) return;

                        if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                        {
                            client.Aisling.GoldPoints += offer;
                            client.Aisling.Inventory.RemoveFromInventory(client, item);
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.PendingItemSessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cWhat a great deal!");
                            TopMenu(client);
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