using System.Data;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Dapper;
using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Sprites;
using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using ServiceStack;

namespace Darkages.Types;

public class Bank : IBank
{
    public Bank()
    {
        Items = new Dictionary<uint, Item>();
    }

    private SemaphoreSlim CreateLock1 { get; } = new(1, 1);
    private SemaphoreSlim CreateLock2 { get; } = new(1, 1);
    private SemaphoreSlim SaveLock { get; } = new(1, 1);
    public Dictionary<uint, Item> Items { get; }

    public async Task<bool> Deposit(WorldClient client, Item item)
    {
        var temp = new Item
        {
            ItemId = item.ItemId,
            Name = item.Name,
            Serial = item.Serial,
            Color = item.Color,
            Cursed = item.Cursed,
            Durability = item.Durability,
            Identified = item.Identified,
            ItemVariance = item.ItemVariance,
            WeapVariance = item.WeapVariance,
            ItemQuality = item.ItemQuality,
            OriginalQuality = item.OriginalQuality,
            Stacks = (ushort)client.PendingBankedSession.ArgsQuantity,
            Enchantable = item.Enchantable,
            Template = item.Template
        };

        if (temp.Template.CanStack)
        {
            await CreateLock1.WaitAsync().ConfigureAwait(false);

            try
            {
                const string procedure = "[CheckIfItemExists]";
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                var cmd = new SqlCommand(procedure, sConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = temp.DisplayName;
                cmd.Parameters.Add("Serial", SqlDbType.Int).Value = client.Aisling.Serial;
                cmd.CommandTimeout = 5;

                var reader = await cmd.ExecuteReaderAsync();
                var itemName = "";
                while (reader.Read())
                {
                    itemName = reader["Name"].ToString();
                    var stacked = (int)reader["Stacks"];

                    if (temp.Stacks + (ushort)stacked > temp.Template.MaxStack)
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Sorry we can't hold that many.");
                        temp.GiveTo(client.Aisling);
                        return false;
                    }

                    temp.Stacks += (ushort)stacked;
                }

                reader.Close();
                sConn.Close();

                if (itemName.IsNullOrEmpty())
                {
                    AddToAislingDb(client.Aisling, temp);
                }
                else
                {
                    await UpdateBanked(client.Aisling, temp);
                }

                return true;
            }
            catch (SqlException e)
            {
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
                CreateLock1.Release();
            }
        }

        AddToAislingDb(client.Aisling, temp);
        return true;
    }

    public async void AddToAislingDb(ISprite aisling, Item item)
    {
        await CreateLock2.WaitAsync().ConfigureAwait(false);

        try
        {
            await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("ItemToBank", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var color = ItemColors.ItemColorsToInt(item.Template.Color);
            var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
            var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
            var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
            var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
            var templateNameReplaced = item.Template.Name;

            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = templateNameReplaced;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
            cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
            cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
            cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
            cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
            cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
            cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
            cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
            cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
            cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;
            cmd.Parameters.Add("@CanStack", SqlDbType.Bit).Value = item.Template.CanStack;
            cmd.Parameters.Add("@Tarnished", SqlDbType.Bit).Value = item.Tarnished;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Item did not save correctly. Contact GM");
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
            CreateLock2.Release();
        }
    }

    public async Task UpdateBanked(ISprite aisling, Item item)
    {
        if (item == null) return;

        await SaveLock.WaitAsync().ConfigureAwait(false);
            
        try
        {
            if (!item.Template.CanStack)
            {
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("BankItemSave", sConn);
                cmd.CommandType = CommandType.StoredProcedure;

                var color = ItemColors.ItemColorsToInt(item.Template.Color);
                var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
                var templateNameReplaced = item.Template.Name;

                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = templateNameReplaced;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
                cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
                cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
                cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
                cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
                cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
                cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
                cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
                cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
                cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
                cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;
                cmd.Parameters.Add("@CanStack", SqlDbType.Bit).Value = item.Template.CanStack;
                cmd.Parameters.Add("@Tarnished", SqlDbType.Bit).Value = item.Tarnished;

                cmd.CommandTimeout = 5;
                cmd.ExecuteNonQuery();
                sConn.Close();
            }
            else
            {
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("BankItemSaveStacked", sConn);
                cmd.CommandType = CommandType.StoredProcedure;

                var color = ItemColors.ItemColorsToInt(item.Template.Color);
                var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
                var templateNameReplaced = item.Template.Name;

                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = templateNameReplaced;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
                cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
                cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
                cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
                cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
                cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
                cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
                cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
                cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
                cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
                cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;
                cmd.Parameters.Add("@CanStack", SqlDbType.Bit).Value = item.Template.CanStack;
                cmd.Parameters.Add("@Tarnished", SqlDbType.Bit).Value = item.Tarnished;

                cmd.CommandTimeout = 5;
                cmd.ExecuteNonQuery();
                sConn.Close();
            }
        }
        catch (SqlException e)
        {
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
        }
    }

    public async Task<bool> Withdraw(WorldClient client, Mundane mundane)
    {
        #region Item Check

        if (client.PendingBankedSession.SelectedItem == null) return false;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(client.PendingBankedSession.SelectedItem.Template.Name)) return false;
        var pullStack = client.PendingBankedSession.SelectedItem.Stacks - client.PendingBankedSession.ArgsQuantity;

        #endregion

        // weight check
        if (client.Aisling.CurrentWeight + client.PendingBankedSession.SelectedItem.Template.CarryWeight > client.Aisling.MaximumWeight)
        {
            mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = mundane.Serial,
                Text = $"{client.PendingBankedSession.SelectedItem.Template.Name} is too heavy for you.",
                Type = 0x03
            });
            return false;
        }

        // max stack check
        if (pullStack > client.PendingBankedSession.SelectedItem.Template.MaxStack && client.PendingBankedSession.SelectedItem.Template.CanStack)
        {
            mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = mundane.Serial,
                Text = "Well, that's impossible. Can you even hold that?",
                Type = 0x03
            });
            return false;
        }

        // banked check
        if (client.PendingBankedSession.ArgsQuantity > client.PendingBankedSession.SelectedItem.Stacks && client.PendingBankedSession.SelectedItem.Template.CanStack)
        {
            mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = mundane.Serial,
                Text = "You don't have that many banked with us.",
                Type = 0x03
            });
            return false;
        }

        // prevent 0 on stacked check
        if (client.PendingBankedSession.ArgsQuantity == 0 && client.PendingBankedSession.SelectedItem.Template.CanStack)
        {
            mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = mundane.Serial,
                Text = "Zero? You sure?",
                Type = 0x03
            });
            return false;
        }

        // dup & fraud check
        if (client.PendingBankedSession.ArgsQuantity < 0 && client.PendingBankedSession.SelectedItem.Template.CanStack)
        {
            mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = mundane.Serial,
                Text = "What? Should I call a guard?",
                Type = 0x03
            });
            return false;
        }

        // stacked more than 1 item check
        if (client.PendingBankedSession.ArgsQuantity > 1 && client.PendingBankedSession.SelectedItem.Stacks > 1 && pullStack != 0)
        {
            // give player item with updated stacks count
            client.PendingBankedSession.SelectedItem.Stacks = (ushort)client.PendingBankedSession.ArgsQuantity;
            client.PendingBankedSession.SelectedItem.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
            client.PendingBankedSession.SelectedItem.GiveTo(client.Aisling);

            // bank item updated with stack count
            client.PendingBankedSession.SelectedItem.Stacks = (ushort)pullStack;
            await UpdateBanked(client.Aisling, client.PendingBankedSession.SelectedItem);
            client.Aisling.BankManager.Items.TryRemove(client.PendingBankedSession.ItemId, out _);
            return true;
        }

        // stacked and 1 item
        if (client.PendingBankedSession.ArgsQuantity == 1 && client.PendingBankedSession.SelectedItem.Stacks == 1)
        {
            var inventoryList = client.Aisling.Inventory.Items;

            foreach (var item in inventoryList)
            {
                if (item.Value == null) continue;
                if (!item.Value.Template.CanStack) continue;
                if (item.Value.Template.Name != client.PendingBankedSession.SelectedItem.Template.Name) continue;
                if (item.Value.Stacks >= item.Value.Template.MaxStack) continue;
                if (item.Value.Stacks < item.Value.Template.MaxStack)
                {
                    // give player item with updated stacks count
                    client.PendingBankedSession.SelectedItem.Stacks = (ushort)client.PendingBankedSession.ArgsQuantity;
                    client.PendingBankedSession.SelectedItem.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
                    client.PendingBankedSession.SelectedItem.GiveTo(client.Aisling);
                    client.Aisling.BankManager.Items.TryRemove(client.PendingBankedSession.ItemId, out _);
                    DeleteFromAislingDb(client);
                    return true;
                }
            }
        }

        // default normal items
        client.PendingBankedSession.SelectedItem.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        client.PendingBankedSession.SelectedItem.GiveTo(client.Aisling);
        client.Aisling.BankManager.Items.TryRemove(client.PendingBankedSession.ItemId, out _);
        DeleteFromAislingDb(client);
        return true;
    }

    public void DeleteFromAislingDb(IWorldClient client)
    {
        if (client.PendingBankedSession.ItemId == 0) return;

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersBanked WHERE ItemId = @ItemId";
            sConn.Execute(cmd, new { client.PendingBankedSession.ItemId });
            sConn.Close();
        }
        catch (SqlException e)
        {
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
    }

    public void DepositGold(IWorldClient client, uint gold)
    {
        client.Aisling.GoldPoints -= gold;
        client.Aisling.BankedGold += gold;
        client.SendStats(StatusFlags.StructC);
    }

    public void WithdrawGold(IWorldClient client, uint gold)
    {
        client.Aisling.GoldPoints += gold;
        client.Aisling.BankedGold -= gold;
        client.SendStats(StatusFlags.StructC);
    }

    public void UpdatePlayersWeight(WorldClient client)
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
}