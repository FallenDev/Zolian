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

[Script("Eireann")]
public class Eireann : MundaneScript
{
    public Eireann(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.QuestManager.EternalLove)
        {
            case false when (client.Aisling.Level >= 50):
                options.Add(new(0x06, "{=qEternal Love"));
                break;
            case true when (!client.Aisling.QuestManager.CryptTerrorSlayed):
                options.Add(new(0x08, "..."));
                break;
        }

        options.Add(new(0x01, "Rumors"));
        options.Add(new(0x02, "Buy"));
        options.Add(new(0x03, "Pawn"));

        client.SendOptionsDialog(Mundane, "Greetings Adventurer, care for some mead?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var gossip = Random.Shared.Next(1, 6);

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
            case 0x01:
                {
                    switch (gossip)
                    {
                        case 1:
                            client.SendOptionsDialog(Mundane, "You can recover from wounds by laying down (Clicking a {=qBed{=a).");
                            break;
                        case 2:
                            client.SendOptionsDialog(Mundane, "Not all quests offer rewards that you can see, some times it's good to just help people.");
                            break;
                        case 3:
                            client.SendOptionsDialog(Mundane, "I overheard that there is a secret shop in Abel... I wonder where?");
                            // ToDo: Make sure this is enabled for aislings other than assassins on entry
                            client.Aisling.QuestManager.AbelShopAccess = true;
                            break;
                        case 4:
                            client.SendOptionsDialog(Mundane, "There are three basement access panels to the crypts.");
                            break;
                        case 5:
                            client.SendOptionsDialog(Mundane, "I heard a rumor that the altar can give you {=bForsaken{=a items if you're really lucky.");
                            break;
                    }
                }
                break;
            case 0x02:
                {
                    client.SendItemShopDialog(Mundane, "Only the finest.", 0x04, ShopMethods.BuyFromStoreInventory(Mundane));
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
                                item = item.Create(client.Aisling, template, Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

                                if (item.GiveTo(client.Aisling))
                                {
                                    client.Aisling.GoldPoints -= template.Value;

                                    client.SendAttributes(StatUpdateType.WeightGold);
                                    client.SendOptionsDialog(Mundane, $"*slides* your {{=c{args} {{=aover to you.");
                                }
                                else
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Yeah right, you're too drunk.");
                                }

                                break;
                            }
                        case false:
                            {
                                client.SendOptionsDialog(Mundane, "You're a mess, I'm cutting you off!");
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
                        var offer = (int)(itemFromSlot.Template.Value / 1.2);

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
                                $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (1.2x)",
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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cBottoms Up!");
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

                        var offer = (uint)(item.Template.Value / 2.8);

                        if (offer <= 0) return;
                        if (offer > item.Template.Value) return;

                        if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                        {
                            client.Aisling.GoldPoints += offer;
                            client.Aisling.Inventory.RemoveFromInventory(client, item);
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.PendingItemSessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cSee you around.");
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