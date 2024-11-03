using Darkages.Common;
using Darkages.Enums;
using Darkages.Managers;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Banker")]
public class Banker(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private readonly BankManager _bankTeller = new();
    private bool _depositGoldCancel;
    private bool _withdrawGoldCancel;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        _depositGoldCancel = false;
        _withdrawGoldCancel = false;
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Inventory.TotalItems > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x03, "Deposit Item"));
        }

        if (!client.Aisling.BankManager.Items.IsEmpty)
        {
            options.Add(new Dialog.OptionsDataItem(0x02, "Withdraw Items"));
            options.Add(new Dialog.OptionsDataItem(0x04, "Withdraw Bundled Items"));
        }

        if (client.Aisling.GoldPoints > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x07, "Deposit Gold"));
        }

        if (client.Aisling.BankedGold > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x0A, "Withdraw Gold"));
        }

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

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (client.Aisling.ActionUsed != "Remote Bank")
            if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                {
                    if (string.IsNullOrEmpty(args)) return;
                    var itemOrSlot = ushort.TryParse(args, out var slot);

                    switch (itemOrSlot)
                    {
                        // Withdrawing
                        case false:
                            StartWithdrawItem(client, args);
                            break;
                        // Depositing
                        case true when slot > 0:
                            StartDepositItem(client, slot);
                            break;
                    }
                }
                break;
            case 0x01:
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

                        if (!client.Aisling.BankManager.Items.IsEmpty)
                        {
                            options.Add(new Dialog.OptionsDataItem(0x02, "Withdraw Item"));
                        }

                        client.SendOptionsDialog(Mundane, "We don't seem to have that. *checks ledger*", options.ToArray());
                        return;
                    }

                    // Withdraw
                    if (client.PendingItemSessions != null)
                    {
                        CompleteStackedWithdraw(client, amount);
                        return;
                    }

                    // Deposit Stacked Item
                    if (client.PendingBuySessions != null)
                    {
                        CompleteStackedDepositItem(client, amount);
                        return;
                    }

                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
                    client.PendingBuySessions = null;
                    client.PendingItemSessions = null;
                    client.CloseDialog();
                }
                else
                {
                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
                    client.PendingBuySessions = null;
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
            case 0x04:
                {
                    client.SendWithdrawBankDialog(Mundane, "Which item bundles do you wish to pull?", NpcShopExtensions.WithdrawStackedFromBank(client));
                }
                break;
            case 0x07:
                if (_depositGoldCancel)
                {
                    client.CloseDialog();
                    return;
                }
                client.SendTextInput(Mundane, $"{{=aInventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}", 0x07, "Deposit:", 10);
                _depositGoldCancel = true;
                break;
            case 0x08:
                {
                    var correctDeposit = ulong.TryParse(args, out var depositAmount);
                    if (correctDeposit)
                    {
                        if (client.Aisling.GoldPoints >= depositAmount)
                            _bankTeller.TempGoldDeposit = depositAmount;
                        else
                        {
                            client.SendOptionsDialog(Mundane, "Looks like you don't have enough, sorry.");
                            return;
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "What was that again?");
                        return;
                    }

                    var depositOptions = new List<Dialog.OptionsDataItem>
                    {
                        new (0x09, "Yes"),
                        new (0x07, "No")
                    };

                    client.SendOptionsDialog(Mundane, $"Ok! So you want to go ahead and deposit {_bankTeller.TempGoldDeposit}", depositOptions.ToArray());
                }
                break;
            case 0x09:
                _bankTeller.DepositGold(client, _bankTeller.TempGoldDeposit);
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {_bankTeller.TempGoldDeposit}");
                client.CloseDialog();
                break;
            case 0x0A:
                if (_withdrawGoldCancel)
                {
                    client.CloseDialog();
                    return;
                }
                client.SendTextInput(Mundane, $"{{=cBanked: {{=q{client.Aisling.BankedGold}\n{{=aInventory: {{=c{client.Aisling.GoldPoints}", 0x0A, "Withdraw:", 10);
                _withdrawGoldCancel = true;
                break;
            case 0x0B:
                var correctWithdraw = ulong.TryParse(args, out var withdrawAmount);
                if (correctWithdraw)
                {
                    if (client.Aisling.BankedGold >= withdrawAmount)
                        _bankTeller.TempGoldWithdraw = withdrawAmount;
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you don't have enough, sorry.");
                        return;
                    }
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "What was that again?");
                    return;
                }

                var withdrawOptions = new List<Dialog.OptionsDataItem>
                {
                    new (0x0C, "Yes"),
                    new (0x0A, "No")
                };

                client.SendOptionsDialog(Mundane, $"Ok! So you want to go ahead and withdraw {_bankTeller.TempGoldWithdraw}", withdrawOptions.ToArray());
                break;
            case 0x0C:
                _bankTeller.WithdrawGold(client, _bankTeller.TempGoldWithdraw);
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cWithdrew: {_bankTeller.TempGoldWithdraw}");
                client.CloseDialog();
                break;
            case 0x500: // OnItemDrop
                {
                    client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var item);

                    if (item == null)
                    {
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Well? Where is it?");
                        return;
                    }

                    if (client.Aisling.Inventory.Items.TryUpdate(item.InventorySlot, null, item))
                        client.SendRemoveItemFromPane(item.InventorySlot);

                    item.ItemPane = Item.ItemPanes.Bank;
                    client.Aisling.BankManager.Items.TryAdd(item.ItemId, item);
                    client.Aisling.Inventory.UpdatePlayersWeight(client);
                    client.SendAttributes(StatUpdateType.WeightGold);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{item.DisplayName}");
                }
                break;
        }
    }

    private void StartDepositItem(WorldClient client, ushort slot)
    {
        client.Aisling.Inventory.Items.TryGetValue(slot, out var inventoryItem);
        if (inventoryItem == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Well? Where is it?");
            client.CloseDialog();
            return;
        }

        if (inventoryItem.Stacks >= 1 && inventoryItem.Template.CanStack)
        {
            client.PendingBuySessions = new PendingBuy
            {
                ID = inventoryItem.ItemId,
                Name = inventoryItem.DisplayName,
                Offer = inventoryItem.InventorySlot,
                Quantity = 0
            };

            client.SendTextInput(Mundane, $"How many would you like to deposit?\nYou currently have: {inventoryItem.Stacks}", "Amount:", 3);
            return;
        }

        if (!client.Aisling.Inventory.Items.TryUpdate(inventoryItem.InventorySlot, null, inventoryItem))
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Well? Where is it?");
            client.CloseDialog();
            return;
        }

        inventoryItem.ItemPane = Item.ItemPanes.Bank;

        if (client.Aisling.BankManager.Items.TryAdd(inventoryItem.ItemId, inventoryItem))
            client.SendRemoveItemFromPane(inventoryItem.InventorySlot);

        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, thank you for trusting {inventoryItem.DisplayName} with us!");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{inventoryItem.DisplayName}");
        client.Aisling.Inventory.UpdatePlayersWeight(client);
        TopMenu(client);
    }

    private void CompleteStackedDepositItem(WorldClient client, ushort amount)
    {
        client.PendingBuySessions.Quantity = amount;
        client.Aisling.Inventory.Items.TryGetValue(client.PendingBuySessions.Offer, out var itemInInv);

        if (itemInInv == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Are you sure?");
            client.PendingBuySessions = null;
            client.CloseDialog();
            return;
        }

        if (amount == 0)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, Zero? Really?");
            client.PendingBuySessions = null;
            client.CloseDialog();
            return;
        }

        if (itemInInv.Stacks < client.PendingBuySessions.Quantity)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, where are they?");
            client.PendingBuySessions = null;
            client.CloseDialog();
            return;
        }

        if (itemInInv.Stacks == client.PendingBuySessions.Quantity)
        {
            var stacked = FindExistingStackAndAdd(client);

            if (stacked)
            {
                client.Aisling.Inventory.Items.TryUpdate(itemInInv.InventorySlot, null, itemInInv);
                client.SendRemoveItemFromPane(itemInInv.InventorySlot);
                StackedDepositCleanup(client, itemInInv);
                return;
            }

            if (!client.Aisling.Inventory.Items.TryUpdate(itemInInv.InventorySlot, null, itemInInv))
            {
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, where is it?");
                client.PendingBuySessions = null;
                client.CloseDialog();
                return;
            }

            itemInInv.ItemPane = Item.ItemPanes.Bank;
            if (client.Aisling.BankManager.Items.TryAdd(itemInInv.ItemId, itemInInv))
                client.SendRemoveItemFromPane(itemInInv.InventorySlot);

            StackedDepositCleanup(client, itemInInv);
            return;
        }

        if (itemInInv.Stacks > client.PendingBuySessions.Quantity)
        {
            // Modify Existing
            client.Aisling.Inventory.RemoveRange(client, itemInInv, client.PendingBuySessions.Quantity);
            var stacked = FindExistingStackAndAdd(client);

            if (stacked)
            {
                StackedDepositCleanup(client, itemInInv);
                return;
            }

            // Create
            var itemCreateFromTemplate = new Item();
            var itemCreated = itemCreateFromTemplate.Create(client.Aisling, itemInInv.Template);
            itemCreated.Stacks = client.PendingBuySessions.Quantity;
            itemCreated.ItemPane = Item.ItemPanes.Bank;

            // Deposit
            client.Aisling.BankManager.Items.TryAdd(itemCreated.ItemId, itemCreated);
            StackedDepositCleanup(client, itemCreated);
        }
    }

    private static bool FindExistingStackAndAdd(WorldClient client)
    {
        client.Aisling.Inventory.Items.TryGetValue(client.PendingBuySessions.Offer, out var itemInInv);
        if (itemInInv == null) return false;

        var bankManager = client.Aisling.BankManager.Items.Values;

        foreach (var itemStored in bankManager)
        {
            if (itemStored.Template.Name != itemInInv.Template.Name) continue;
            var overStacked = itemStored.Stacks + client.PendingBuySessions.Quantity > itemStored.Template.MaxStack;
            if (overStacked) continue;
            itemStored.Stacks += client.PendingBuySessions.Quantity;
            client.Aisling.BankManager.Items.TryUpdate(itemStored.ItemId, itemStored, itemStored);
            return true;
        }

        return false;
    }

    private void StackedDepositCleanup(WorldClient client, Item bankedItem)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{bankedItem.DisplayName}, x{client.PendingBuySessions.Quantity}");
        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, thank you for trusting {bankedItem.DisplayName} with us!");
        client.Aisling.Inventory.UpdatePlayersWeight(client);
        client.PendingBuySessions = null;
        TopMenu(client);
    }

    private void StartWithdrawItem(WorldClient client, string args)
    {
        // Obtain the first item to check if its stackable etc
        var bankToInv = client.Aisling.BankManager.Items.Values.FirstOrDefault(x => x.NoColorDisplayName == args);
        if (bankToInv == null) return;
        if (bankToInv.Stacks >= 1 && bankToInv.Template.CanStack)
        {
            // Set the name only
            client.PendingItemSessions = new PendingSell
            {
                ID = 0,
                Name = bankToInv.DisplayName,
                Quantity = 0
            };

            client.SendTextInput(Mundane, "How many would you like back?", "Amount:", 3);
            return;
        }

        // Handle non-stackable
        client.Aisling.BankManager.Items.TryRemove(bankToInv.ItemId, out var verifiedItem);
        if (verifiedItem == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I'm sorry it seems we don't have that.");
            TopMenu(client);
            return;
        }

        var itemGiven = verifiedItem.GiveTo(client.Aisling);
        if (!itemGiven)
        {
            client.Aisling.BankManager.Items.TryAdd(verifiedItem.ItemId, verifiedItem);
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, seems you can't hold it.");
            return;
        }

        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {verifiedItem.DisplayName ?? ""}");
        TopMenu(client);
    }

    /// <summary>
    /// Look at how inventory combines stacks and use that logic here with bank stacks
    /// </summary>
    /// <param name="client"></param>
    /// <param name="amount"></param>
    private void CompleteStackedWithdraw(WorldClient client, ushort amount)
    {
        client.PendingItemSessions.Quantity = amount;

        // Search for an IDs large enough to accommodate capacity requested
        var matchingItems = client.Aisling.BankManager.Items.Values.Where(itemBanked => itemBanked.DisplayName == client.PendingItemSessions.Name).Where(itemBanked => itemBanked.Stacks >= amount).ToList();

        // Pick the smallest stack capable of fulfilling the request
        var bestFit = matchingItems.MinBy(i => i.Stacks);

        if (bestFit == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I'm sorry it seems you don't have that.");
            TopMenu(client);
            return;
        }

        client.PendingItemSessions.ID = bestFit.ItemId;

        // Grab the item
        client.Aisling.BankManager.Items.TryGetValue(client.PendingItemSessions.ID, out var itemInBank);

        #region Validation Checks
        
        if (itemInBank == null)
        {
            client.SendOptionsDialog(Mundane, "We don't seem to have that. *checks ledger*");
            return;
        }

        if (amount == 0)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
            client.PendingItemSessions = null;
            client.CloseDialog();
            return;
        }

        if (itemInBank.Stacks < client.PendingItemSessions.Quantity)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
            client.PendingItemSessions = null;
            client.CloseDialog();
            return;
        }

        #endregion

        if (itemInBank.Stacks == client.PendingItemSessions.Quantity)
        {
            var removed = client.Aisling.BankManager.Items.TryRemove(itemInBank.ItemId, out var verifiedItem);

            if (!removed)
            {
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I'm sorry it seems you don't have that.");
                TopMenu(client);
                return;
            }

            if (verifiedItem == null)
            {
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I'm sorry it seems you don't have that.");
                TopMenu(client);
                return;
            }

            var itemGiven = verifiedItem.GiveTo(client.Aisling);
            if (!itemGiven)
            {
                verifiedItem.ItemPane = Item.ItemPanes.Bank;
                client.Aisling.BankManager.Items.TryAdd(verifiedItem.ItemId, verifiedItem);
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, looks like you can't hold that. I'll hold onto it.");
                return;
            }

            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {verifiedItem.DisplayName ?? ""} x{verifiedItem.Stacks}");
            client.Aisling.Inventory.UpdatePlayersWeight(client);
            client.PendingItemSessions = null;
            TopMenu(client);
            return;
        }

        // If the stack in the bank has leftover quantities
        if (itemInBank.Stacks > client.PendingItemSessions.Quantity)
        {
            // Modify Existing
            itemInBank.Stacks -= client.PendingItemSessions.Quantity;
            client.Aisling.BankManager.Items.TryUpdate(itemInBank.ItemId, itemInBank, itemInBank);

            // Create
            var itemCreateFromTemplate = new Item();
            var itemCreated = itemCreateFromTemplate.Create(client.Aisling, itemInBank.Template);
            itemCreated.Stacks = client.PendingItemSessions.Quantity;

            // Give
            var itemGiven = itemCreated.GiveTo(client.Aisling);
            if (!itemGiven)
            {
                itemInBank.Stacks += client.PendingItemSessions.Quantity;
                client.Aisling.BankManager.Items.TryUpdate(itemInBank.ItemId, itemInBank, itemInBank);
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, looks like you can't hold that. I'll hold onto it.");
                return;
            }

            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {itemCreated.DisplayName} x{itemCreated.Stacks}");
            client.Aisling.Inventory.UpdatePlayersWeight(client);
            client.PendingItemSessions = null;
            TopMenu(client);
        }
    }
}