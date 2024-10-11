using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Loures;

[Script("Rezti")]
public class Rezti(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
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
            new(0x02, "Buy"),
            new(0x03, "Sell")
        };

        if (client.Aisling.QuestManager.ArmorApothecaryAccepted && !client.Aisling.QuestManager.ArmorCodexDeciphered && !client.Aisling.HasItem("Aosda Transcriptions Volume: IV"))
            options.Add(new(0x06, "Hello, I was told you could help with this?"));

        if (client.Aisling.HasItem("Aosda Transcriptions Volume: IV"))
            options.Add(new(0x07, "Is this the right transcriptions?"));

        client.SendOptionsDialog(Mundane,
            !client.Aisling.QuestManager.ArmorCraftingCodexLearned
                ? "*glances up* How can I help you?"
                : $"Ah, {client.Aisling.Username}. How did your codex work come along?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
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
                        client.SendOptionsDialog(Mundane, "I'm sorry, freshly sold out.");
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
                client.SendItemShopDialog(Mundane, "Here's what I have to offer.", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x03:
                client.SendItemSellDialog(Mundane, "What do you want to sell?", NpcShopExtensions.GetCharacterSellInventoryByteList(client));
                break;
            case 0x05:
                client.CloseDialog();
                break;
            case 0x06:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "On it!"),
                        new (0x05, "What do I look like, a librarian?")
                    };

                    client.SendOptionsDialog(Mundane, $"Ancient Aosda Writing?! Please head to the library and obtain a book for me" +
                                                      $" Look in the back right, you'll see a stack of blue books. I need decipher text to help", options.ToArray());
                    break;
                }
            case 0x07:
                {
                    client.Aisling.Inventory.RemoveFromInventory(client, client.Aisling.HasItemReturnItem("Aosda Transcriptions Volume: IV"));

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x08, "Wow, you're fast!")
                    };

                    client.SendOptionsDialog(Mundane, $"Yes! Don't go anywhere, it will take just a few seconds for me to translate this... " +
                                                      $"Yes, you're going to be very happy with this news. I've just about finished, one moment.", options.ToArray());
                    break;
                }
            case 0x08:
                {
                    client.Aisling.QuestManager.ArmorCodexDeciphered = true;
                    client.Aisling.QuestManager.LouresReputation++;
                    var tablet = new Item();
                    tablet = tablet.Create(client.Aisling, "Transcribed Armorsmithing Tablet");
                    tablet.GiveTo(client.Aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, client.Aisling.HasItemReturnItem("Ancient Smithing Codex"));
                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmCodex1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG4,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Transcribed an Ancient Armor Codex"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x05, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "Here, I've deciphered the codex. I'm going to keep the original here, I'll return to you the transcribed version.", options.ToArray());
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
                                var given = itemCreated.GiveTo(client.Aisling);
                                if (!given)
                                {
                                    client.Aisling.BankManager.Items.TryAdd(itemCreated.ItemId, itemCreated);
                                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
                                }
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
                    client.PendingBuySessions = null;
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
                    NpcShopExtensions.AutoSellItemDroppedFromInventory(client, Mundane, args);
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

    public override void OnGossip(WorldClient client, string message) { }
}