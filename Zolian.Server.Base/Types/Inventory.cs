using System.Collections.Concurrent;
using System.Data;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Object;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types
{
    public class Inventory : ObjectManager, IInventory
    {
        private const int Length = 59;

        public readonly ConcurrentDictionary<int, Item> Items = new();
        private SemaphoreSlim SaveLock { get; } = new(1, 1);

        public Inventory()
        {
            for (var i = 0; i < Length; i++) Items[i + 1] = null;
        }

        public IEnumerable<byte> BankList => (Items.Where(i => i.Value is {Template: { }} && i.Value.Template.Flags.FlagIsSet(ItemFlags.Bankable))).Select(i => i.Value.InventorySlot);

        public int TotalItems => Items.Count;
        
        public bool CanPickup(Aisling player, Item lpItem)
        {
            if (player == null || lpItem == null) return false;
            if (lpItem.Template == null) return false;

            if (lpItem.Stacks <= 1)
                return player.CurrentWeight + lpItem.Template.CarryWeight < player.MaximumWeight &&
                       FindEmpty() != byte.MaxValue;

            var weight = lpItem.Template.CarryWeight * lpItem.Stacks;
            return player.CurrentWeight + weight < player.MaximumWeight && FindEmpty() != byte.MaxValue;
        }

        public byte FindEmpty()
        {
            byte idx = 1;

            foreach (var slot in Items)
            {
                if (slot.Value == null)
                    return idx;

                idx++;
            }

            return byte.MaxValue;
        }

        public Item FindInSlot(int slot)
        {
            return Items.TryGetValue(slot, out var item) ? item : null;
        }

        public new IEnumerable<Item> Get(Predicate<Item> prediate)
        {
            return Items.Values.Where(i => i != null && prediate(i)).ToArray();
        }

        public List<Item> HasMany(Predicate<Item> predicate)
        {
            return Items.Values.Where(i => i != null && predicate(i)).ToList();
        }

        public Item Has(Predicate<Item> predicate)
        {
            return Items.Values.FirstOrDefault(i => i != null && predicate(i));
        }

        public int Has(Template templateContext)
        {
            var items = Items.Where(i => i.Value != null && i.Value.Template.Name == templateContext.Name)
                .Select(i => i.Value).ToList();

            var anyItem = items.FirstOrDefault();

            if (anyItem?.Template == null)
                return 0;

            var result = anyItem.Template.CanStack ? items.Sum(i => i.Stacks) : items.Count;

            return result;
        }

        public int HasCount(Template templateContext)
        {
            var items = Items.Where(i => i.Value != null && i.Value.Template.Name == templateContext.Name)
                .Select(i => i.Value).ToList();

            return items.Count;
        }

        public void Remove(GameClient client, Item item)
        {
            if (item == null)
                return;

            if (Remove(item.InventorySlot) != null) client.Send(new ServerFormat10(item.InventorySlot));
            client.SendStats(StatusFlags.StructA);
            item.DeleteFromAislingDb();
        }

        public Item Remove(byte movingFrom)
        {
            if (Items.ContainsKey(movingFrom))
            {
                var copy = Items[movingFrom];
                Items[movingFrom] = null;
                return copy;
            }

            return null;
        }

        public void RemoveFromInventory(GameClient client, Item item)
        {
            if (item != null && client.Aisling.Inventory.Remove(item.InventorySlot) == null) return;
            if (item == null) return;

            client.Send(new ServerFormat10(item.InventorySlot));
            client.LastItemDropped = item;
            client.SendStats(StatusFlags.StructA);
            item.DeleteFromAislingDb();
        }

        public void RemoveRange(GameClient client, Item item, int range)
        {
            var remaining = Math.Abs(item.Stacks - range);

            if (remaining <= 0)
            {
                RemoveFromInventory(client, item);
                item.Remove();
            }
            else
            {
                item.Stacks = (ushort) remaining;
                client.Send(new ServerFormat10(item.InventorySlot));
                client.Aisling.Inventory.Set(item);
                UpdateSlot(client, item);
            }

            UpdatePlayersWeight(client);
        }

        public void AddRange(GameClient client, Item item, int range)
        {
            var given = Math.Abs(item.Stacks + range);

            if (given <= 0)
            {
                RemoveFromInventory(client, item);
                item.Remove();
            }
            else
            {
                item.Stacks = (ushort)given;
                client.Send(new ServerFormat10(item.InventorySlot));
                client.Aisling.Inventory.Set(item);
                UpdateSlot(client, item);
            }

            UpdatePlayersWeight(client);
        }

        public void Set(Item s)
        {
            if (s == null) return;

            if (Items.ContainsKey(s.InventorySlot)) Items[s.InventorySlot] = s;
        }

        public void UpdateSlot(GameClient client, Item item)
        {
            UpdateInventory(client.Aisling);
            item.Scripts = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);
            if (!string.IsNullOrEmpty(item.Template.WeaponScript))
                item.WeaponScripts = ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);
            client.Send(new ServerFormat0F(item));
            UpdatePlayersWeight(client);
        }

        public void UpdatePlayersWeight(GameClient client)
        {
            client.Aisling.CurrentWeight = 0;

            foreach (var inventory in client.Aisling.Inventory.Items)
            {
                if (inventory.Value == null) continue;
                if (inventory.Value.Stacks > 1)
                {
                    for (var i = 0; i < inventory.Value.Stacks; i++)
                    {
                        client.Aisling.CurrentWeight += inventory.Value.Template.CarryWeight;
                    }
                }
                else
                {
                    client.Aisling.CurrentWeight += inventory.Value.Template.CarryWeight;
                }
            }

            foreach (var equipment in client.Aisling.EquipmentManager.Equipment)
            {
                if (equipment.Value?.Slot == 0) continue;
                if (equipment.Value?.Item == null) continue;
                client.Aisling.CurrentWeight += equipment.Value.Item.Template.CarryWeight;
            }
        }

        public async void UpdateInventory(Aisling obj)
        {
            if (obj?.Inventory == null) return;

            await SaveLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                foreach (var item in obj.Inventory.Items.Values.Where(i => i != null && i.InventorySlot != 0))
                {
                    var cmd = new SqlCommand("InventorySave", sConn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    var color = ItemColors.ItemColorsToInt(item.Template.Color);
                    var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                    var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                    var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                    var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);

                    cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
                    cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = item.Template.Name;
                    cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = obj.Serial;
                    cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
                    cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
                    cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
                    cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
                    cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
                    cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
                    cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
                    cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
                    cmd.Parameters.Add("@InventorySlot", SqlDbType.Int).Value = item.InventorySlot;
                    cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
                    cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;

                    cmd.CommandTimeout = 5;
                    cmd.ExecuteNonQuery();
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                if (e.Message.Contains("PK__Players"))
                {
                    obj.Client.SendMessage(0x03, "Your inventory did not save. Contact GM (Code: DeadSea)");
                    Crashes.TrackError(e);
                    return;
                }

                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            finally
            {
                SaveLock.Release();
                UpdatePlayersWeight(obj.Client);
            }
        }
    }
}