using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Dungeon Shop")]
public class DungeonShop : MundaneScript
{
    public DungeonShop(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>
        {
            //new (0x01, "Dungeon Rumors"),
            new (0x02, "Buy"),
            new (0x03, "Pawn")
        };

        client.SendOptionsDialog(Mundane, "Greetings Adventurer, look no further.\nI have exactly what you're looking for.\nAll items purchased have a quality range of (Common => Rare)", options.ToArray());
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
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
                            client.SendOptionsDialog(Mundane, $"Ah, one of our best items.\n{{=c{args}{{=a, it will serve you well.");
                        }
                        else
                        {
                            client.SendMessage(0x02, "Yeah right, you can't even physically hold it.");
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
                        client.SendStats(StatusFlags.WeightMoney);
                        client.PendingBuySessions = null;
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cHope it serves you well.");
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
                        client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                        client.SendStats(StatusFlags.WeightMoney);
                        client.PendingItemSessions = null;
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cWhat a great deal!");
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