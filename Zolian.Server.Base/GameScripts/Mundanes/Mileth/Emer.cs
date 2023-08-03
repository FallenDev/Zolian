using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
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
            new (0x02, "Buy"),
            new (0x03, "Sell")
        };

        switch (client.Aisling.QuestManager.KillerBee)
        {
            case false:
                {
                    if (client.Aisling.Level >= 11 || client.Aisling.GameMaster)
                    {
                        options.Add(new Dialog.OptionsDataItem(0x04, "Killer Bee Jam"));
                    }

                    break;
                }
            case true:
                options.Add(new Dialog.OptionsDataItem(0x07, "Here's the corpse"));
                break;
        }

        client.SendOptionsDialog(Mundane, "Are ye hungry?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                {
                    if (string.IsNullOrEmpty(args)) return;
                    var itemOrSlot = ushort.TryParse(args, out var slot);

                    switch (itemOrSlot)
                    {
                        // Buying
                        case false:
                            NpcShopExtensions.BuyItemFromInventory(client, Mundane, args);
                            break;
                        // Selling
                        case true when slot > 0:
                            NpcShopExtensions.SellItemFromInventory(client, Mundane, args);
                            break;
                    }
                }
                break;
            case 0x01: // Follows Sequence to buy a stacked item from the vendor
                var containsInt = ushort.TryParse(args, out var amount);
                if (containsInt)
                {
                    if (client.PendingBuySessions == null && client.PendingItemSessions == null)
                    {
                        client.SendOptionsDialog(Mundane, "We just ran out.");
                        return;
                    }

                    if (client.PendingBuySessions != null)
                    {
                        client.PendingBuySessions.Quantity = amount;
                        NpcShopExtensions.BuyStackedItemFromInventory(client, Mundane);
                    }

                    if (client.PendingItemSessions != null)
                    {
                        client.PendingItemSessions.Quantity = amount;
                        NpcShopExtensions.SellStackedItemFromInventory(client, Mundane);
                    }
                }
                break;
            case 0x02:
                client.SendItemShopDialog(Mundane, "Bon Appetite.", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x03:
                client.SendItemSellDialog(Mundane, "What do you want to sell?", NpcShopExtensions.GetCharacterSellInventoryByteList(client));
                break;
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "I'll take care of them."),
                        new (0x06, "Sorry, I'm busy.")
                    };

                    client.SendOptionsDialog(Mundane, $"Killer bees in the Eastern Woodlands have been causing our town some issues. Please go to \n{{=qPath 2 {{=a and bring me back a {{=vKiller Bee's Corpse{{=a.", options.ToArray());
                }
                break;
            case 0x05:
                {
                    client.SendOptionsDialog(Mundane, "Thank you, it'll sure make some wicked jam.");
                    client.Aisling.QuestManager.KillerBee = true;
                    break;
                }
            case 0x06:
                {
                    client.SendOptionsDialog(Mundane, "Well someone has to do something..");
                    break;
                }
            case 0x07:
                {
                    if (client.Aisling.HasItem("Killer Bee Corpse"))
                    {
                        client.Aisling.QuestManager.KillerBee = false;
                        client.TakeAwayQuantity(client.Aisling, "Killer Bee Corpse", 1);
                        client.GiveExp(2500);
                        var item = new Item();
                        item = item.Create(client.Aisling, "Honey Bacon Burger", Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
                        item.GiveTo(client.Aisling);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 2,500 experience.");
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.SendOptionsDialog(Mundane, "Lovely, I'll take as many as you have. Here's a taste of what I make from it.");
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"Have you killed any yet? Bring back proof.");
                    }

                    break;
                }
            case 0x19:
                {
                    if (client.PendingBuySessions != null)
                    {
                        var quantity = client.PendingBuySessions.Quantity;
                        var item = client.PendingBuySessions.Name;
                        var cost = (uint)(client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity);
                        if (client.Aisling.GoldPoints >= cost)
                        {
                            client.Aisling.GoldPoints -= cost;
                            if (client.PendingBuySessions.Quantity > 1)
                                client.GiveQuantity(client.Aisling, item, quantity);
                            else
                            {
                                var itemCreated = new Item();
                                var template = ServerSetup.Instance.GlobalItemTemplateCache[item];
                                itemCreated = itemCreated.Create(client.Aisling, template,
                                    NpcShopExtensions.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(),
                                    ItemQualityVariance.DetermineWeaponVariance());
                                itemCreated.GiveTo(client.Aisling);
                            }
                            client.SendAttributes(StatUpdateType.Primary);
                            client.SendAttributes(StatUpdateType.ExpGold);
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
                            client.SendAttributes(StatUpdateType.Primary);
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.PendingItemSessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
                            TopMenu(client);
                        }
                    }
                }
                break;
            case 0x20:
                {
                    client.PendingItemSessions = null;
                    client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
                }
                break;
            case 0x30:
                {
                    if (client.PendingItemSessions != null)
                    {
                        NpcShopExtensions.CompletePendingItemSell(client, Mundane);
                    }

                    TopMenu(client);
                }
                break;
            case 0x500:
                {
                    NpcShopExtensions.SellItemDroppedFromInventory(client, Mundane, args);
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
            OnResponse(client, 0x500, item.InventorySlot.ToString());
        }
    }
}