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

[Script("Banker")]
public class Banker : MundaneScript
{
    private Bank _bankTeller = new();

    public Banker(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Inventory.TotalItems > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x03, "Deposit Item"));
        }

        if (client.Aisling.BankManager.Items.Count > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x02, "Withdraw Item"));
        }

        //if (client.Aisling.GoldPoints > 0)
        //{
        //    options.Add(new Dialog.OptionsDataItem(0x07, "Deposit Gold"));
        //}

        //if (client.Aisling.BankedGold > 0)
        //{
        //    options.Add(new Dialog.OptionsDataItem(0x08, "Withdraw Gold"));
        //}

        client.SendOptionsDialog(Mundane, "Don't mind the goblins, they help around here", options.ToArray());
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Bankable))
        {
            OnResponse(client, 0x500, item.InventorySlot.ToString());
        }
    }

    public override void OnGoldDropped(WorldClient client, uint money)
    {
        if (client == null) return;
        if (money <= client.Aisling.GoldPoints)
        {
            _bankTeller.DepositGold(client, money);
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"Hail! We'll take your deposit of {money} coin(s)");
        }
        else
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "I'm sorry it seems you don't have that much");
        }
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
                        // Withdrawing
                        case false:
                            var bankToInv = client.Aisling.BankManager.Items.Values.FirstOrDefault(x => x.NoColorDisplayName == args);
                            if (bankToInv != null)
                            {
                                if (bankToInv.Stacks == 1 && bankToInv.Template.CanStack)
                                {
                                    client.PendingItemSessions = new PendingSell
                                    {
                                        ID = bankToInv.ItemId,
                                        Name = bankToInv.DisplayName,
                                        Quantity = 0
                                    };

                                    client.SendTextInput(Mundane, "How many would you like back?", "Amount:", 3);
                                    break;
                                }
                                client.Aisling.BankManager.Items.TryRemove(bankToInv.ItemId, out var verifiedItem);
                                verifiedItem?.GiveTo(client.Aisling);
                                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {verifiedItem?.DisplayName ?? ""}");
                                TopMenu(client);
                            }

                            break;
                        // Depositing
                        case true when slot > 0:
                            client.Aisling.Inventory.Items.TryGetValue(slot, out var inventoryItem);
                            if (inventoryItem == null)
                            {
                                client.SendOptionsDialog(Mundane, "Well? Where is it?");
                                client.PendingBuySessions = null; // might not need this yet
                                return;
                            }
                            break;
                    }
                }
                break;
            case 0x01: // Follows Sequence to withdraw a stacked item from the vendor
                var containsInt = ushort.TryParse(args, out var amount);

                if (containsInt)
                {
                    if (client.PendingBuySessions == null && client.PendingItemSessions == null)
                    {
                        var options = new List<Dialog.OptionsDataItem>();

                        if (client.Aisling.Inventory.TotalItems > 0)
                        {
                            options.Add(new Dialog.OptionsDataItem(0x03, "Deposit Item"));
                        }

                        if (client.Aisling.BankManager.Items.Count > 0)
                        {
                            options.Add(new Dialog.OptionsDataItem(0x02, "Withdraw Item"));
                        }

                        client.SendOptionsDialog(Mundane, "We don't seem to have that. *checks ledger*", options.ToArray());
                        return;
                    }
                    
                    // Withdraw
                    if (client.PendingItemSessions != null)
                    {
                        client.PendingItemSessions.Quantity = amount;
                        client.Aisling.BankManager.Items.TryGetValue(client.PendingItemSessions.ID, out var itemInBank);

                        if (itemInBank == null)
                        {
                            client.SendOptionsDialog(Mundane, "We don't seem to have that. *checks ledger*");
                            return;
                        }

                        if (amount == 0)
                        {
                            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats in the ledger");
                            client.PendingItemSessions = null;
                            client.CloseDialog();
                            return;
                        }

                        // Check stack size. If more than they have, display error and clear pendingitem, display public message that they don't have that much
                        if (itemInBank.Stacks < client.PendingItemSessions.Quantity)
                        {
                            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats in the ledger");
                            client.PendingItemSessions = null;
                            client.CloseDialog();
                            return;
                        }

                        // Check stack size. If equal, remove from bankmanager, and mimic logic above for a full item, clear pendingitem
                        if (itemInBank.Stacks == client.PendingItemSessions.Quantity)
                        {
                            client.Aisling.BankManager.Items.TryRemove(itemInBank.ItemId, out var verifiedItem);
                            verifiedItem?.GiveTo(client.Aisling);
                            verifiedItem?.DeleteFromAislingDb();
                            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {verifiedItem?.DisplayName ?? ""} x{verifiedItem?.Stacks}");
                            client.PendingItemSessions = null;
                            TopMenu(client);
                            return;
                        }

                        // Check stack size. If less than they have, tryUpdate bankmanger minus pendingitem.Quantity, then create a new item with the pendingitem data
                        // send giveTo, send public message, clear pendingitem
                        if (itemInBank.Stacks > client.PendingItemSessions.Quantity)
                        {
                            // Modify Existing
                            var tempItem = itemInBank;
                            tempItem.Stacks -= client.PendingItemSessions.Quantity;
                            client.Aisling.BankManager.Items.TryUpdate(itemInBank.ItemId, tempItem, itemInBank);

                            // Create
                            var itemCreateFromTemplate = new Item();
                            var itemCreated = itemCreateFromTemplate.Create(client.Aisling, itemInBank.Template);
                            itemCreated.Stacks = client.PendingItemSessions.Quantity;

                            // Give
                            itemCreated.GiveTo(client.Aisling);
                            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {itemCreated.DisplayName} x{itemCreated.Stacks}");
                            client.PendingItemSessions = null;
                            TopMenu(client);
                        }
                    }
                }
                else
                {
                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats in the ledger");
                    client.PendingItemSessions = null;
                    client.CloseDialog();
                }
                break;
            case 0x02:
                {
                    client.SendWithdrawBankDialog(Mundane, "What do you wish to withdraw? *holds ledger out*", NpcShopExtensions.WithdrawFromBank(client));
                }
                break;
            case 0x03:
                {
                    client.SendItemSellDialog(Mundane, "We deploy and guard all vaults with the strongest guards", NpcShopExtensions.GetCharacterBankInventoryByteList(client));
                }
                break;
            case 0x19:
                {
                    
                }
                break;
            case 0x20:
                {

                }
                break;
            case 0x30:
                {
                    
                }
                break;
            case 0x500: // OnItemDrop
                {
                    var containsUshort = ushort.TryParse(args, out var slot);
                    if (containsUshort)
                    {
                        var item = client.Aisling.Inventory.FindInSlot(slot);
                        if (item == null)
                        {
                            client.SendOptionsDialog(Mundane, "Well? Where is it?");
                            client.PendingBuySessions = null;
                            return;
                        }

                        if (!client.Aisling.Inventory.Items.TryUpdate(item.InventorySlot, null, item))
                        {
                            client.SendOptionsDialog(Mundane, "Well? Where is it?");
                            client.PendingBuySessions = null;
                            return;
                        }

                        item.ItemPane = Item.ItemPanes.Bank;
                        if (client.Aisling.BankManager.Items.TryAdd(item.ItemId, item))
                            client.SendRemoveItemFromPane(item.InventorySlot);

                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{item.DisplayName}");
                        client.Aisling.Inventory.UpdatePlayersWeight(client);
                    }
                    else
                    {
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, hmm, you sure?");
                    }
                }
                break;
        }
    }
}