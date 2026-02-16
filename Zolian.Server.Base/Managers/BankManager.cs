using Darkages.Network.Client.Abstractions;
using Darkages.Sprites.Entity;
using System.Collections.Concurrent;
using System.Data;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Microsoft.Data.SqlClient;
using Darkages.Database;

namespace Darkages.Managers;

public class BankManager
{
    public ConcurrentDictionary<long, Item> Items { get; } = [];
    public ulong TempGoldDeposit { get; set; }
    public ulong TempGoldWithdraw { get; set; }

    public void DepositGold(IWorldClient client, ulong gold)
    {
        client.Aisling.GoldPoints -= gold;
        client.Aisling.BankedGold += gold;
        client.SendAttributes(StatUpdateType.ExpGold);
        client.Aisling.PlayerSaveDirty = true;
    }

    public void WithdrawGold(IWorldClient client, ulong gold)
    {
        client.Aisling.GoldPoints += gold;
        client.Aisling.BankedGold -= gold;
        client.SendAttributes(StatUpdateType.ExpGold);
        client.Aisling.PlayerSaveDirty = true;
    }

    /// <summary>
    /// Removes the item from bank (Only used when item is pawned in bulk) -
    /// Deletes the record from the database
    /// </summary>
    public static async Task RemoveFromBankAsync(WorldClient client, List<Item> itemsToDelete, CancellationToken ct = default)
    {
        if (client == null) return;
        if (itemsToDelete == null || itemsToDelete.Count == 0) return;
        var iDt = ItemsDataTable();

        try
        {
            foreach (var item in itemsToDelete)
            {
                var pane = ItemEnumConverters.PaneToString(item.ItemPane);
                var color = ItemColors.ItemColorsToInt(item.Template.Color);
                var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
                var gearEnhanced = ItemEnumConverters.GearEnhancementToString(item.GearEnhancement);
                var itemMaterial = ItemEnumConverters.ItemMaterialToString(item.ItemMaterial);

                iDt.Rows.Add(
                    item.ItemId,
                    item.Template.Name,
                    (long)client.Aisling.Serial,
                    pane,
                    item.Slot,
                    item.InventorySlot,
                    color,
                    item.Cursed,
                    item.Durability,
                    item.Identified,
                    itemVariance,
                    weapVariance,
                    quality,
                    orgQuality,
                    item.Stacks,
                    item.Enchantable,
                    item.Tarnished,
                    gearEnhanced,
                    itemMaterial,
                    item.GiftWrapped
                );
            }

            using (await StorageManager.AislingBucket.GetPlayerLock(client.Aisling.Serial).LockAsync().ConfigureAwait(false))
            {
                await DeleteFromAislingDbAsync(iDt, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Issue pawning items from {client.RemoteIp} - {client.Aisling.Serial}");
            SentrySdk.CaptureException(ex);
        }

        client.Aisling.PlayerSaveDirty = true;
    }

    private static DataTable ItemsDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("ItemId", typeof(long));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Serial", typeof(long)); // Owner's Serial
        dt.Columns.Add("ItemPane", typeof(string));
        dt.Columns.Add("Slot", typeof(int));
        dt.Columns.Add("InventorySlot", typeof(int));
        dt.Columns.Add("Color", typeof(int));
        dt.Columns.Add("Cursed", typeof(bool));
        dt.Columns.Add("Durability", typeof(long));
        dt.Columns.Add("Identified", typeof(bool));
        dt.Columns.Add("ItemVariance", typeof(string));
        dt.Columns.Add("WeapVariance", typeof(string));
        dt.Columns.Add("ItemQuality", typeof(string));
        dt.Columns.Add("OriginalQuality", typeof(string));
        dt.Columns.Add("Stacks", typeof(int));
        dt.Columns.Add("Enchantable", typeof(bool));
        dt.Columns.Add("Tarnished", typeof(bool));
        dt.Columns.Add("GearEnhancement", typeof(string));
        dt.Columns.Add("ItemMaterial", typeof(string));
        dt.Columns.Add("GiftWrapped", typeof(string));
        return dt;
    }

    private static async Task DeleteFromAislingDbAsync(DataTable iDt, CancellationToken ct)
    {
        await using var conn = new SqlConnection(AislingStorage.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new SqlCommand("ItemMassDelete", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        var p = cmd.Parameters.AddWithValue("@Items", iDt);
        p.SqlDbType = SqlDbType.Structured;
        p.TypeName = "dbo.ItemType";
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}