using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.NorthPole;

[Script("Yule Revenge")]
public class Yeti(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private long _repairSum;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.PendingItemSessions = null;
        client.PendingBuySessions = null;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>
        {
            new (0x02, "Buy"),
            new (0x03, "Sell"),
            new (0x04, "Repair all Items")
        };

        if (client.Aisling.QuestManager.YetiKilled)
        {
            client.SendOptionsDialog(Mundane, "Wow! You took care of the Yeti! You must be strong.", opts.ToArray());
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: *dances*");
            return;
        }

        if (client.Aisling.HasKilled("Yeti", 1))
        {
            opts.Add(new(0x06, "It is done"));
        }
        else
        {
            opts.Add(new(0x05, "How can I help?"));
        }

        client.SendOptionsDialog(Mundane, "The Yeti is coming, and unless he is stopped. Christmas will not happen this year.", opts.ToArray());
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
            case 0x04:
                {
                    _repairSum = NpcShopExtensions.GetRepairCosts(client);

                    var optsRepair = new List<Dialog.OptionsDataItem>
                    {
                        new(0x14, ServerSetup.Instance.Config.MerchantConfirmMessage),
                        new(0x15, ServerSetup.Instance.Config.MerchantCancelMessage)
                    };

                    if (_repairSum == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Your items are in good condition, no repairs are necessary.");
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "It will cost {=c" + _repairSum + "{=a Gold to repair everything. Do you Agree?", _repairSum.ToString(), optsRepair.ToArray());
                    }
                }
                break;
            case 0x05:
                client.SendOptionsDialog(Mundane, "Becareful, somewhere deep in this cavern is the formidable Yeti. Every year he tries to interfere with " +
                                                  "us and our preparations for Christmas. This year he's been extra naughty. As he's turned Frosty against us!");

                break;
            case 0x06:
                {
                    if (client.Aisling.HasKilled("Yeti", 1))
                    {
                        client.Aisling.QuestManager.YetiKilled = true;
                        client.GiveItem("Stocking Stuffer");
                        client.SendAttributes(StatUpdateType.WeightGold);

                        var legend = new Legend.LegendItem
                        {
                            Key = "LYeti1",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.RedPurpleG2,
                            Icon = (byte)LegendIcon.Heart,
                            Text = "Thwarted the Yeti's Plot on Christmas"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Oh, it looks like he's still alive.");
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "You're so strong to make it back with nothing but a scratch! Here, take this.");
                }
                break;
            case 0x14:
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
            case 0x15:
                client.SendOptionsDialog(Mundane, "Come back before anything breaks.");
                break;
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
}