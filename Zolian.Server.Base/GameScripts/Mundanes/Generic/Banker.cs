using System.Data;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Managers;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Logging;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Banker")]
public class Banker(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    // Dialog/response IDs
    private const ushort Response_Main = 0x00;
    private const ushort Response_AmountEntry = 0x01;

    private const ushort Response_WithdrawMenu = 0x02;
    private const ushort Response_DepositMenu = 0x03;
    private const ushort Response_WithdrawBundled = 0x04;

    private const ushort Response_DepositGoldPrompt = 0x07;
    private const ushort Response_DepositGoldConfirmPrompt = 0x08;
    private const ushort Response_DepositGoldConfirmYes = 0x09;

    private const ushort Response_WithdrawGoldPrompt = 0x0A;
    private const ushort Response_WithdrawGoldConfirmPrompt = 0x0B;
    private const ushort Response_WithdrawGoldConfirmYes = 0x0C;

    private const ushort Response_DepositItemDrop = 0x500;

    private const ushort Response_PawnMenu = 0x99;
    private const ushort Response_PawnConfirm = 0x991;

    private readonly BankManager _bankTeller = new();
    private bool _depositGoldCancel;
    private bool _withdrawGoldCancel;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        ShowTopMenu(client);
    }

    protected override void TopMenu(WorldClient client) => ShowTopMenu(client);

    private void ShowTopMenu(WorldClient client)
    {
        base.TopMenu(client);

        _depositGoldCancel = false;
        _withdrawGoldCancel = false;

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Inventory.TotalItems > 0)
            options.Add(new Dialog.OptionsDataItem(Response_DepositMenu, "Deposit Item"));

        if (!client.Aisling.BankManager.Items.IsEmpty)
        {
            options.Add(new Dialog.OptionsDataItem(Response_WithdrawMenu, "Withdraw Items"));
            options.Add(new Dialog.OptionsDataItem(Response_WithdrawBundled, "Withdraw Bundled Items"));
        }

        if (client.Aisling.GoldPoints > 0)
            options.Add(new Dialog.OptionsDataItem(Response_DepositGoldPrompt, "Deposit Gold"));

        if (client.Aisling.BankedGold > 0)
            options.Add(new Dialog.OptionsDataItem(Response_WithdrawGoldPrompt, "Withdraw Gold"));

        if (client.Aisling.ActionUsed != "Remote Bank")
            options.Add(new Dialog.OptionsDataItem(Response_PawnMenu, "Pawn Banked Items"));

        client.SendOptionsDialog(Mundane, "Don't mind the goblins, they help around here", options.ToArray());
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null || item == null)
            return;

        if (item.Template.Flags.FlagIsSet(ItemFlags.Bankable))
            OnResponse(client, Response_DepositItemDrop, item.InventorySlot.ToString());
    }

    public override void OnGoldDropped(WorldClient client, uint money)
    {
        if (client == null)
            return;

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

    public override async void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!IsAuthorized(client))
            return;

        switch (responseId)
        {
            case Response_Main:
                HandleMainResponse(client, args);
                break;

            case Response_AmountEntry:
                HandleAmountEntry(client, args);
                break;

            case Response_WithdrawMenu:
                client.SendWithdrawBankDialog(Mundane, "What do you wish to withdraw? *holds ledger out*", NpcShopExtensions.WithdrawFromBank(client));
                break;

            case Response_DepositMenu:
                client.SendItemSellDialog(Mundane, "We deploy and guard all vaults with the strongest guards", NpcShopExtensions.GetCharacterBankInventoryByteList(client));
                break;

            case Response_WithdrawBundled:
                client.SendWithdrawBankDialog(Mundane, "Which item bundles do you wish to pull?", NpcShopExtensions.WithdrawStackedFromBank(client));
                break;

            case Response_DepositGoldPrompt:
                HandleDepositGoldPrompt(client);
                break;

            case Response_DepositGoldConfirmPrompt:
                HandleDepositGoldConfirmPrompt(client, args);
                break;

            case Response_DepositGoldConfirmYes:
                _bankTeller.DepositGold(client, _bankTeller.TempGoldDeposit);
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {_bankTeller.TempGoldDeposit}");
                client.CloseDialog();
                break;

            case Response_WithdrawGoldPrompt:
                HandleWithdrawGoldPrompt(client);
                break;

            case Response_WithdrawGoldConfirmPrompt:
                HandleWithdrawGoldConfirmPrompt(client, args);
                break;

            case Response_WithdrawGoldConfirmYes:
                _bankTeller.WithdrawGold(client, _bankTeller.TempGoldWithdraw);
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cWithdrew: {_bankTeller.TempGoldWithdraw}");
                client.CloseDialog();
                break;

            case Response_DepositItemDrop:
                await HandleDepositItemDropAsync(client, args).ConfigureAwait(false);
                break;

            case Response_PawnMenu:
                ShowPawnMenu(client);
                break;

            case Response_PawnConfirm:
                await HandlePawnConfirmAsync(client).ConfigureAwait(false);
                break;
        }
    }

    private bool IsAuthorized(WorldClient client)
    {
        if (client?.Aisling == null)
            return false;

        if (client.Aisling.ActionUsed == "Remote Bank")
            return true;

        return AuthenticateUser(client);
    }

    private void HandleMainResponse(WorldClient client, string args)
    {
        if (string.IsNullOrEmpty(args))
            return;

        // If args parses as a slot => deposit flow, else => withdraw flow (by item name)
        if (ushort.TryParse(args, out var slot) && slot > 0)
        {
            StartDepositItem(client, slot);
            return;
        }

        StartWithdrawItem(client, args);
    }

    private void HandleAmountEntry(WorldClient client, string args)
    {
        if (!ushort.TryParse(args, out var amount))
        {
            FailLedger(client);
            return;
        }

        // No pending state -> bounce back to menu
        if (client.PendingBuySessions == null && client.PendingItemSessions == null)
        {
            var options = new List<Dialog.OptionsDataItem>();

            if (client.Aisling.Inventory.TotalItems > 0)
                options.Add(new Dialog.OptionsDataItem(Response_DepositMenu, "Deposit Item"));

            if (!client.Aisling.BankManager.Items.IsEmpty)
                options.Add(new Dialog.OptionsDataItem(Response_WithdrawMenu, "Withdraw Item"));

            client.SendOptionsDialog(Mundane, "We don't seem to have that. *checks ledger*", options.ToArray());
            return;
        }

        // Withdraw stacked
        if (client.PendingItemSessions != null)
        {
            CompleteStackedWithdraw(client, amount);
            return;
        }

        // Deposit stacked
        if (client.PendingBuySessions != null)
        {
            CompleteStackedDepositItem(client, amount);
            return;
        }

        FailLedger(client);
    }

    private void FailLedger(WorldClient client)
    {
        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
        client.PendingBuySessions = null;
        client.PendingItemSessions = null;
        client.CloseDialog();
    }

    private void HandleDepositGoldPrompt(WorldClient client)
    {
        if (_depositGoldCancel)
        {
            client.CloseDialog();
            return;
        }

        client.SendTextInput(
            Mundane,
            $"{{=aInventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}",
            Response_DepositGoldPrompt,
            "Deposit:",
            10);

        _depositGoldCancel = true;
    }

    private void HandleDepositGoldConfirmPrompt(WorldClient client, string args)
    {
        if (!ulong.TryParse(args, out var depositAmount))
        {
            client.SendOptionsDialog(Mundane, "What was that again?");
            return;
        }

        if (client.Aisling.GoldPoints < depositAmount)
        {
            client.SendOptionsDialog(Mundane, "Looks like you don't have enough, sorry.");
            return;
        }

        _bankTeller.TempGoldDeposit = depositAmount;

        var depositOptions = new List<Dialog.OptionsDataItem>
        {
            new(Response_DepositGoldConfirmYes, "Yes"),
            new(Response_DepositGoldPrompt, "No")
        };

        client.SendOptionsDialog(Mundane, $"Ok! So you want to go ahead and deposit {_bankTeller.TempGoldDeposit}", depositOptions.ToArray());
    }

    private void HandleWithdrawGoldPrompt(WorldClient client)
    {
        if (_withdrawGoldCancel)
        {
            client.CloseDialog();
            return;
        }

        client.SendTextInput(
            Mundane,
            $"{{=cBanked: {{=q{client.Aisling.BankedGold}\n{{=aInventory: {{=c{client.Aisling.GoldPoints}",
            Response_WithdrawGoldPrompt,
            "Withdraw:",
            10);

        _withdrawGoldCancel = true;
    }

    private void HandleWithdrawGoldConfirmPrompt(WorldClient client, string args)
    {
        if (!ulong.TryParse(args, out var withdrawAmount))
        {
            client.SendOptionsDialog(Mundane, "What was that again?");
            return;
        }

        if (client.Aisling.BankedGold < withdrawAmount)
        {
            client.SendOptionsDialog(Mundane, "Looks like you don't have enough, sorry.");
            return;
        }

        _bankTeller.TempGoldWithdraw = withdrawAmount;

        var withdrawOptions = new List<Dialog.OptionsDataItem>
        {
            new(Response_WithdrawGoldConfirmYes, "Yes"),
            new(Response_WithdrawGoldPrompt, "No")
        };

        client.SendOptionsDialog(Mundane, $"Ok! So you want to go ahead and withdraw {_bankTeller.TempGoldWithdraw}", withdrawOptions.ToArray());
    }

    private async Task HandleDepositItemDropAsync(WorldClient client, string args)
    {
        if (!int.TryParse(args, out var invSlot))
            return;

        client.Aisling.Inventory.Items.TryGetValue(invSlot, out var item);

        if (item == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Well? Where is it?");
            return;
        }

        if (item.Template.CanStack)
        {
            await DepositStackedPersistThenCacheAsync(client, item, item.Stacks).ConfigureAwait(false);
        }
        else
        {
            // Non-stackable: cache update then write-through update
            if (client.Aisling.Inventory.Items.TryUpdate(item.InventorySlot, null, item))
                client.SendRemoveItemFromPane(item.InventorySlot);

            item.ItemPane = Item.ItemPanes.Bank;
            item.Slot = 0;
            item.InventorySlot = 0;

            client.Aisling.BankManager.Items.TryAdd(item.ItemId, item);
            await PersistBankDepositNonStackAsync(client.Aisling.Serial, item.ItemId).ConfigureAwait(false);
        }

        client.Aisling.Inventory.UpdatePlayersWeight(client);
        client.SendAttributes(StatUpdateType.WeightGold);
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{item.DisplayName}");
    }

    #region Deposit Logic

    private void StartDepositItem(WorldClient client, ushort slot)
    {
        client.Aisling.Inventory.Items.TryGetValue(slot, out var inventoryItem);

        if (inventoryItem == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Well? Where is it?");
            client.CloseDialog();
            return;
        }

        if (inventoryItem.Template.CanStack && inventoryItem.Stacks >= 1)
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
        ShowTopMenu(client);
    }

    private async void CompleteStackedDepositItem(WorldClient client, ushort amount)
    {
        client.PendingBuySessions!.Quantity = amount;
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

        if (itemInInv.Stacks < amount)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, where are they?");
            client.PendingBuySessions = null;
            client.CloseDialog();
            return;
        }

        await DepositStackedPersistThenCacheAsync(client, itemInInv, amount).ConfigureAwait(false);
        StackedDepositCleanup(client, itemInInv);
    }

    private async Task DepositStackedPersistThenCacheAsync(WorldClient client, Item sourceItem, ushort amount)
    {
        // Atomic per-player.
        using (await StorageManager.AislingBucket.GetPlayerLock(client.Aisling.Serial).LockAsync().ConfigureAwait(false))
        {
            var serial = client.Aisling.Serial;
            var sourceItemId = sourceItem.ItemId;

            // Used only for partial-deposit split that must create a NEW bank row.
            long? newBankItemId = amount < sourceItem.Stacks ? EphemeralRandomIdGenerator<long>.Shared.NextId : null;

            await ExecuteBankDepositStackAsync(
                    serial: serial,
                    sourceItemId: sourceItemId,
                    quantity: amount,
                    maxStack: sourceItem.Template.MaxStack,
                    newBankItemId: newBankItemId)
                .ConfigureAwait(false);

            await ReconcileStackedDepositFromDbAsync(client, sourceItem.Template.Name, sourceItemId).ConfigureAwait(false);
        }
    }

    private static async Task ExecuteBankDepositStackAsync(uint serial, long sourceItemId, int quantity, int maxStack, long? newBankItemId)
    {
        await using var conn = new SqlConnection(AislingStorage.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new SqlCommand("dbo.BankDepositStack", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
        cmd.Parameters.Add("@SourceItemId", SqlDbType.BigInt).Value = sourceItemId;
        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
        cmd.Parameters.Add("@MaxStack", SqlDbType.Int).Value = maxStack;

        var pNewId = cmd.Parameters.Add("@NewBankItemId", SqlDbType.BigInt);
        pNewId.Value = newBankItemId.HasValue ? newBankItemId.Value : DBNull.Value;

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task ReconcileStackedDepositFromDbAsync(WorldClient client, string itemName, long sourceItemId)
    {
        var serial = (long)client.Aisling.Serial;

        await using var conn = new SqlConnection(AislingStorage.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        const string bankSql =
            "SELECT ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, " +
            "       ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished, " +
            "       GearEnhancement, ItemMaterial, GiftWrapped " +
            "FROM dbo.PlayersItems " +
            "WHERE Serial = @Serial AND ItemPane = 'Bank' AND Name = @Name " +
            "ORDER BY Stacks ASC, ItemId ASC;";

        const string sourceSql =
            "SELECT ItemId, ItemPane, Stacks " +
            "FROM dbo.PlayersItems " +
            "WHERE Serial = @Serial AND ItemId = @ItemId;";

        var bankRows = new List<DbItemRow>();

        await using (var bankCmd = new SqlCommand(bankSql, conn))
        {
            bankCmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            bankCmd.Parameters.Add("@Name", SqlDbType.VarChar, 45).Value = itemName;

            await using var reader = await bankCmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
                bankRows.Add(DbItemRow.FromReader(reader));
        }

        DbSourceRow? sourceRow = null;

        await using (var srcCmd = new SqlCommand(sourceSql, conn))
        {
            srcCmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            srcCmd.Parameters.Add("@ItemId", SqlDbType.BigInt).Value = sourceItemId;

            await using var r = await srcCmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await r.ReadAsync().ConfigureAwait(false))
                sourceRow = new DbSourceRow(r.GetInt64(0), r.GetString(1), r.GetInt32(2));
        }

        // Inventory reconcile (by ItemId)
        var paneIsInventory = sourceRow != null
                              && ItemEnumConverters.StringToPane(sourceRow.Value.ItemPane) == Item.ItemPanes.Inventory;

        if (sourceRow == null || !paneIsInventory)
        {
            var invEntry = client.Aisling.Inventory.Items
                .FirstOrDefault(kvp => kvp.Value != null && kvp.Value.ItemId == sourceItemId);

            if (invEntry.Value != null)
            {
                client.Aisling.Inventory.Items.TryUpdate(invEntry.Key, null, invEntry.Value);
                client.SendRemoveItemFromPane(Convert.ToByte(invEntry.Key));
            }
        }
        else
        {
            var dbStacks = (ushort)Math.Max(0, sourceRow.Value.Stacks);

            var invItem = client.Aisling.Inventory.Items
                .FirstOrDefault(kvp => kvp.Value != null && kvp.Value.ItemId == sourceItemId).Value;

            if (invItem != null && invItem.Stacks != dbStacks)
            {
                invItem.Stacks = dbStacks;
                client.Aisling.Inventory.UpdateSlot(client, invItem);
            }
        }

        // Bank reconcile (keyed by ItemId)
        var dbBankIds = bankRows.Select(r => r.ItemId).ToHashSet();

        var removeIds = client.Aisling.BankManager.Items
            .Where(kvp => kvp.Value?.Template != null
                          && string.Equals(kvp.Value.Template.Name, itemName, StringComparison.Ordinal)
                          && !dbBankIds.Contains(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in removeIds)
            client.Aisling.BankManager.Items.TryRemove(id, out _);

        foreach (var row in bankRows)
        {
            if (!client.Aisling.BankManager.Items.TryGetValue(row.ItemId, out var bankItem) || bankItem == null)
            {
                var created = CreateItemFromDbRow(client.Aisling, row);
                client.Aisling.BankManager.Items[row.ItemId] = created;
            }
            else
            {
                ApplyDbRowToExistingItem(bankItem, row);
                client.Aisling.BankManager.Items.TryUpdate(row.ItemId, bankItem, bankItem);
            }
        }
    }

    private static void ApplyDbRowToExistingItem(Item item, DbItemRow row)
    {
        if (item.Template == null || !string.Equals(item.Template.Name, row.Name, StringComparison.Ordinal))
        {
            if (ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(row.Name, out var template))
                item.Template = template;
        }

        var color = item.Template != null
            ? (byte)ItemColors.ItemColorsToInt(item.Template.Color)
            : item.Color;

        item.ItemId = row.ItemId;
        item.Serial = (uint)row.Serial;

        item.ItemPane = ItemEnumConverters.StringToPane(row.ItemPane);
        item.Slot = (byte)row.Slot;
        item.InventorySlot = (byte)row.InventorySlot;

        item.Color = color;
        item.Cursed = row.Cursed;
        item.Durability = (uint)row.Durability;
        item.Identified = row.Identified;

        item.ItemVariance = ItemEnumConverters.StringToArmorVariance(row.ItemVariance);
        item.WeapVariance = ItemEnumConverters.StringToWeaponVariance(row.WeapVariance);
        item.ItemQuality = ItemEnumConverters.StringToQuality(row.ItemQuality);
        item.OriginalQuality = ItemEnumConverters.StringToQuality(row.OriginalQuality);

        item.Stacks = (ushort)row.Stacks;

        item.Enchantable = item.Template?.Enchantable ?? row.Enchantable;
        item.Tarnished = row.Tarnished;
        item.GearEnhancement = ItemEnumConverters.StringToGearEnhancement(row.GearEnhancement);
        item.ItemMaterial = ItemEnumConverters.StringToItemMaterial(row.ItemMaterial);
        item.GiftWrapped = row.GiftWrapped;

        if (item.Template != null)
        {
            item.Image = item.Template.Image;
            item.DisplayImage = item.Template.DisplayImage;
        }

        ItemQualityVariance.SetMaxItemDurability(item, item.ItemQuality);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
    }

    private static Item CreateItemFromDbRow(Aisling aisling, DbItemRow row)
    {
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(row.Name, out var template))
            throw new InvalidOperationException($"Template not found for item '{row.Name}'.");

        var color = (byte)ItemColors.ItemColorsToInt(template.Color);

        var item = new Item
        {
            ItemId = row.ItemId,
            Template = template,
            Serial = (uint)row.Serial,
            ItemPane = ItemEnumConverters.StringToPane(row.ItemPane),
            Slot = (byte)row.Slot,
            InventorySlot = (byte)row.InventorySlot,
            Color = color,
            Cursed = row.Cursed,
            Durability = (uint)row.Durability,
            Identified = row.Identified,
            ItemVariance = ItemEnumConverters.StringToArmorVariance(row.ItemVariance),
            WeapVariance = ItemEnumConverters.StringToWeaponVariance(row.WeapVariance),
            ItemQuality = ItemEnumConverters.StringToQuality(row.ItemQuality),
            OriginalQuality = ItemEnumConverters.StringToQuality(row.OriginalQuality),
            Stacks = (ushort)row.Stacks,
            Enchantable = template.Enchantable,
            Tarnished = row.Tarnished,
            GearEnhancement = ItemEnumConverters.StringToGearEnhancement(row.GearEnhancement),
            ItemMaterial = ItemEnumConverters.StringToItemMaterial(row.ItemMaterial),
            GiftWrapped = row.GiftWrapped,
            Image = template.Image,
            DisplayImage = template.DisplayImage
        };

        ItemQualityVariance.SetMaxItemDurability(item, item.ItemQuality);
        item.GetDisplayName();
        item.NoColorGetDisplayName();

        return item;
    }

    private static async Task PersistBankDepositNonStackAsync(uint serial, long itemId)
    {
        using (await StorageManager.AislingBucket.GetPlayerLock(serial).LockAsync().ConfigureAwait(false))
        {
            await using var conn = new SqlConnection(AislingStorage.ConnectionString);
            await conn.OpenAsync().ConfigureAwait(false);

            const string sql =
                "UPDATE ZolianPlayers.dbo.PlayersItems " +
                "SET ItemPane = 'Bank', Slot = 0, InventorySlot = 0 " +
                "WHERE ItemId = @ItemId AND Serial = @Serial;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ItemId", SqlDbType.BigInt).Value = itemId;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    private void StackedDepositCleanup(WorldClient client, Item bankedItem)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDeposited: {{=g{bankedItem.DisplayName}, x{client.PendingBuySessions!.Quantity}");
        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, thank you for trusting {bankedItem.DisplayName} with us!");
        client.Aisling.Inventory.UpdatePlayersWeight(client);

        client.PendingBuySessions = null;
        ShowTopMenu(client);
    }

    #endregion

    #region Pawn Logic

    private void ShowPawnMenu(WorldClient client)
    {
        var options = new List<Dialog.OptionsDataItem> { new(Response_PawnConfirm, "Let's proceed") };

        client.SendOptionsDialog(
            Mundane,
            "You wish to use our Goblin pawning resources? That's great! What we'll do is " +
            "{=csell all of your items of damaged or common quality {=ato Gobleregan. " +
            "Note that what the goblins give us, is up to debate.. Are you certain you want to proceed?",
            options.ToArray());

        client.SendServerMessage(
            ServerMessageType.NonScrollWindow,
            "Armor, Helmets, Quest Related, and items that aren't sellable or droppable will be {=bignored{=a.\n\n" +
            "Additionally items that have been enhanced to an advanced level will be {=bignored{=a.");
    }

    private async Task HandlePawnConfirmAsync(WorldClient client)
    {
        uint offer = 0;
        var itemsToDelete = new List<Item>();

        foreach (var (_, item) in client.Aisling.BankManager.Items)
        {
            if (item.OriginalQuality >= Item.Quality.Uncommon) continue;
            if (item.Template.CanStack) continue;
            if (!item.Template.Flags.FlagIsSet(ItemFlags.Dropable)) continue;
            if (!item.Template.Flags.FlagIsSet(ItemFlags.Sellable)) continue;
            if (item.Template.Flags.FlagIsSet(ItemFlags.QuestRelated)) continue;
            if (item.Template.ScriptName is "Armor" or "Helmet") continue;
            if (item.ItemMaterial != Item.ItemMaterials.None) continue;
            if (item.GearEnhancement != Item.GearEnhancements.None) continue;

            offer += item.Template.Value / 5;
            client.Aisling.BankManager.Items.TryRemove(item.ItemId, out _);
            itemsToDelete.Add(item);
        }

        await BankManager.RemoveFromBankAsync(client, itemsToDelete).ConfigureAwait(false);

        client.Aisling.BankedGold += offer;
        client.SendOptionsDialog(Mundane, $"They're going to send over your {offer} gold and we'll store it here in the bank.");
    }

    #endregion

    #region Withdraw Logic

    private void StartWithdrawItem(WorldClient client, string args)
    {
        if (client == null) return;
        if (string.IsNullOrWhiteSpace(args)) return;

        // UI passes NoColorDisplayName currently.
        var bankToInv = client.Aisling.BankManager.Items.Values.FirstOrDefault(x => x != null && x.NoColorDisplayName == args);
        if (bankToInv == null)
            return;

        // Stacked -> prompt for amount (DB-first will happen on completion)
        if (bankToInv.Template.CanStack && bankToInv.Stacks >= 1)
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = 0,
                Name = bankToInv.DisplayName,
                Quantity = 0
            };

            client.SendTextInput(Mundane, "How many would you like back?", "Amount:", 3);
            return;
        }

        // Non-stacked: still cache-first (intentionally unchanged here).
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

    private async void CompleteStackedWithdraw(WorldClient client, ushort amount)
    {
        if (client == null) return;
        if (client.PendingItemSessions == null)
        {
            client.CloseDialog();
            return;
        }

        if (amount == 0)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, that's not whats on the ledger");
            client.PendingItemSessions = null;
            client.CloseDialog();
            return;
        }

        client.PendingItemSessions.Quantity = amount;

        // Stable best-fit: smallest stacks, then ItemId.
        var bestFit = client.Aisling.BankManager.Items.Values
            .Where(i => i != null)
            .Where(i => i.Template != null && i.Template.CanStack)
            .Where(i => i.DisplayName == client.PendingItemSessions.Name)
            .Where(i => i.Stacks >= amount)
            .OrderBy(i => i.Stacks)
            .ThenBy(i => i.ItemId)
            .FirstOrDefault();

        if (bestFit == null)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I'm sorry it seems you don't have that.");
            client.PendingItemSessions = null;
            TopMenu(client);
            return;
        }

        var itemName = bestFit.Template.Name;
        var maxStack = bestFit.Template.MaxStack;

        // Rail #1: cannot pull more than one stack at a time
        if (amount > maxStack)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal,
                "Sorry you can't pull more than what I can carry at a time.");
            client.PendingItemSessions = null;
            TopMenu(client);
            return;
        }

        // Determine if we can merge fully into an existing inventory stack.
        // Because of Rail #1, we only need ONE stack with enough room.
        var canMergeIntoExisting = client.Aisling.Inventory.Items.Values
            .Where(i => i?.Template != null && i.Template.CanStack)
            .Where(i => string.Equals(i.Template.Name, itemName, StringComparison.Ordinal))
            .Any(i => (i.Stacks + amount) <= maxStack);

        // If we cannot merge, we REQUIRE an empty slot up-front.
        byte? emptySlotOrNull = null;
        if (!canMergeIntoExisting)
        {
            var emptySlot = client.Aisling.Inventory.FindEmpty();
            if (emptySlot == byte.MaxValue)
            {
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal,
                    $"{client.Aisling.Username}, your inventory is full.");
                client.PendingItemSessions = null;
                TopMenu(client);
                return;
            }

            emptySlotOrNull = emptySlot;
        }

        client.PendingItemSessions.ID = bestFit.ItemId;

        try
        {
            await WithdrawStackedPersistThenCacheAsync(client, itemName, bestFit.ItemId, amount, bestFit.Template.MaxStack, emptySlotOrNull).ConfigureAwait(false);

            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here you go.");
            client.Aisling.Inventory.UpdatePlayersWeight(client);
            client.PendingItemSessions = null;
            TopMenu(client);
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Stacked withdraw failed for {client.Aisling.Username} ({client.Aisling.Serial}): {ex.Message}", LogLevel.Error);
            SentrySdk.CaptureException(ex);

            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, something went wrong. Try again.");
            client.PendingItemSessions = null;
            client.CloseDialog();
        }
    }

    private static async Task WithdrawStackedPersistThenCacheAsync(
        WorldClient client,
        string itemName,
        long sourceBankItemId,
        ushort quantity,
        byte maxStack,
        byte? newInventorySlotOrNull)
    {
        using (await StorageManager.AislingBucket.GetPlayerLock(client.Aisling.Serial).LockAsync().ConfigureAwait(false))
        {
            // If we need to create a NEW inventory row, caller provided a slot.
            // If it can merge, caller passed null and SQL will merge or throw if it can't.
            var newInventoryItemId = EphemeralRandomIdGenerator<long>.Shared.NextId;

            await ExecuteBankWithdrawStackAsync(
                    serial: client.Aisling.Serial,
                    sourceBankItemId: sourceBankItemId,
                    quantity: quantity,
                    itemName: itemName,
                    maxStack: maxStack,
                    newInventoryItemId: newInventoryItemId,
                    newInventorySlot: newInventorySlotOrNull)
                .ConfigureAwait(false);

            await ReconcileStackedWithdrawFromDbAsync(client, itemName)
                .ConfigureAwait(false);
        }
    }

    private static async Task ExecuteBankWithdrawStackAsync(
        uint serial,
        long sourceBankItemId,
        ushort quantity,
        string itemName,
        int maxStack,
        long newInventoryItemId,
        byte? newInventorySlot)
    {
        await using var conn = new SqlConnection(AislingStorage.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new SqlCommand("dbo.BankWithdrawStack", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
        cmd.Parameters.Add("@SourceBankItemId", SqlDbType.BigInt).Value = sourceBankItemId;
        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = (int)quantity;
        cmd.Parameters.Add("@Name", SqlDbType.VarChar, 45).Value = itemName;
        cmd.Parameters.Add("@MaxStack", SqlDbType.Int).Value = maxStack;

        cmd.Parameters.Add("@NewInventoryItemId", SqlDbType.BigInt).Value = newInventoryItemId;

        var pSlot = cmd.Parameters.Add("@NewInventorySlot", SqlDbType.TinyInt);
        pSlot.Value = newInventorySlot.HasValue ? newInventorySlot.Value : DBNull.Value;

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task ReconcileStackedWithdrawFromDbAsync(WorldClient client, string itemName)
    {
        var serial = (long)client.Aisling.Serial;

        await using var conn = new SqlConnection(AislingStorage.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        const string inventorySql =
            "SELECT ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, " +
            "       ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished, " +
            "       GearEnhancement, ItemMaterial, GiftWrapped " +
            "FROM dbo.PlayersItems " +
            "WHERE Serial = @Serial AND ItemPane = 'Inventory' AND Name = @Name " +
            "ORDER BY InventorySlot ASC;";

        const string bankSql =
            "SELECT ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, " +
            "       ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished, " +
            "       GearEnhancement, ItemMaterial, GiftWrapped " +
            "FROM dbo.PlayersItems " +
            "WHERE Serial = @Serial AND ItemPane = 'Bank' AND Name = @Name " +
            "ORDER BY Stacks ASC, ItemId ASC;";

        var invRows = new List<DbItemRow>();
        var bankRows = new List<DbItemRow>();

        await using (var invCmd = new SqlCommand(inventorySql, conn))
        {
            invCmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            invCmd.Parameters.Add("@Name", SqlDbType.VarChar, 45).Value = itemName;

            await using var r = await invCmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await r.ReadAsync().ConfigureAwait(false))
                invRows.Add(DbItemRow.FromReader(r));
        }

        await using (var bankCmd = new SqlCommand(bankSql, conn))
        {
            bankCmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            bankCmd.Parameters.Add("@Name", SqlDbType.VarChar, 45).Value = itemName;

            await using var r = await bankCmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await r.ReadAsync().ConfigureAwait(false))
                bankRows.Add(DbItemRow.FromReader(r));
        }

        // Bank cache: remove missing + upsert truth
        var dbBankIds = bankRows.Select(r => r.ItemId).ToHashSet();

        var removeBankIds = client.Aisling.BankManager.Items
            .Where(kvp => kvp.Value != null
                          && kvp.Value.Template != null
                          && string.Equals(kvp.Value.Template.Name, itemName, StringComparison.Ordinal)
                          && !dbBankIds.Contains(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in removeBankIds)
            client.Aisling.BankManager.Items.TryRemove(id, out _);

        foreach (var row in bankRows)
        {
            if (!client.Aisling.BankManager.Items.TryGetValue(row.ItemId, out var bankItem) || bankItem == null)
            {
                var created = CreateItemFromDbRow(client.Aisling, row);
                client.Aisling.BankManager.Items[row.ItemId] = created;
            }
            else
            {
                ApplyDbRowToExistingItem(bankItem, row);
                client.Aisling.BankManager.Items.TryUpdate(row.ItemId, bankItem, bankItem);
            }
        }

        // Inventory cache: remove missing + upsert truth (inventory is keyed by slot)
        var dbInvBySlot = invRows.ToDictionary(r => r.InventorySlot, r => r);

        var invSlotsToClear = client.Aisling.Inventory.Items
            .Where(kvp => kvp.Value != null
                          && kvp.Value.Template != null
                          && string.Equals(kvp.Value.Template.Name, itemName, StringComparison.Ordinal)
                          && !dbInvBySlot.ContainsKey(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var slot in invSlotsToClear)
        {
            if (client.Aisling.Inventory.Items.TryGetValue(slot, out var existing) && existing != null)
            {
                client.Aisling.Inventory.Items.TryUpdate(slot, null, existing);
                client.SendRemoveItemFromPane((byte)slot);
            }
        }

        foreach (var row in invRows)
        {
            var slot = row.InventorySlot;

            if (!client.Aisling.Inventory.Items.TryGetValue(slot, out var invItem) || invItem == null)
            {
                var created = CreateItemFromDbRow(client.Aisling, row);
                client.Aisling.Inventory.Items[slot] = created;
                client.Aisling.Inventory.UpdateSlot(client, created);
            }
            else
            {
                ApplyDbRowToExistingItem(invItem, row);
                client.Aisling.Inventory.Items.TryUpdate(slot, invItem, invItem);
                client.Aisling.Inventory.UpdateSlot(client, invItem);
            }
        }
    }

    #endregion

    // -----------------------------
    // DB Row models
    // -----------------------------
    private readonly record struct DbItemRow(
        long ItemId,
        string Name,
        long Serial,
        string ItemPane,
        int Slot,
        int InventorySlot,
        int Color,
        bool Cursed,
        long Durability,
        bool Identified,
        string ItemVariance,
        string WeapVariance,
        string ItemQuality,
        string OriginalQuality,
        int Stacks,
        bool Enchantable,
        bool Tarnished,
        string GearEnhancement,
        string ItemMaterial,
        string? GiftWrapped)
    {
        public static DbItemRow FromReader(SqlDataReader r) =>
            new(
                ItemId: r.GetInt64(0),
                Name: r.GetString(1),
                Serial: r.GetInt64(2),
                ItemPane: r.GetString(3),
                Slot: r.GetInt32(4),
                InventorySlot: r.GetInt32(5),
                Color: r.GetInt32(6),
                Cursed: r.GetBoolean(7),
                Durability: r.GetInt64(8),
                Identified: r.GetBoolean(9),
                ItemVariance: r.GetString(10),
                WeapVariance: r.GetString(11),
                ItemQuality: r.GetString(12),
                OriginalQuality: r.GetString(13),
                Stacks: r.GetInt32(14),
                Enchantable: r.GetBoolean(15),
                Tarnished: r.GetBoolean(16),
                GearEnhancement: r.GetString(17),
                ItemMaterial: r.GetString(18),
                GiftWrapped: r.IsDBNull(19) ? null : r.GetString(19)
            );
    }

    private readonly record struct DbSourceRow(long ItemId, string ItemPane, int Stacks);
}