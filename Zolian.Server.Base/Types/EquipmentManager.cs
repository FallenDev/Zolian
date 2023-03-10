using System.Collections.Concurrent;
using System.Data;

using Dapper;

using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types;

public class EquipmentManager
{
    public EquipmentManager(GameClient client)
    {
        Client = client;
        Equipment = new ConcurrentDictionary<int, EquipmentSlot>();

        for (byte i = 1; i < 18; i++)
            Equipment[i] = null;
    }

    public GameClient Client { get; set; }
    public int Length => Equipment?.Count ?? 0;
    public ConcurrentDictionary<int, EquipmentSlot> Equipment { get; set; }
    private SemaphoreSlim CreateLock { get; } = new(1, 1);

    public EquipmentSlot Weapon => Equipment[ItemSlots.Weapon];
    public EquipmentSlot Armor => Equipment[ItemSlots.Armor];
    public EquipmentSlot Shield => Equipment[ItemSlots.Shield];
    public EquipmentSlot Helmet => Equipment[ItemSlots.Helmet];
    public EquipmentSlot Earring => Equipment[ItemSlots.Earring];
    public EquipmentSlot Necklace => Equipment[ItemSlots.Necklace];
    public EquipmentSlot LHand => Equipment[ItemSlots.LHand];
    public EquipmentSlot RHand => Equipment[ItemSlots.RHand];
    public EquipmentSlot LArm => Equipment[ItemSlots.LArm];
    public EquipmentSlot RArm => Equipment[ItemSlots.RArm];
    public EquipmentSlot Waist => Equipment[ItemSlots.Waist];
    public EquipmentSlot Leg => Equipment[ItemSlots.Leg];
    public EquipmentSlot Foot => Equipment[ItemSlots.Foot];
    public EquipmentSlot FirstAcc => Equipment[ItemSlots.FirstAcc];
    public EquipmentSlot Trousers => Equipment[ItemSlots.Trousers];
    public EquipmentSlot Coat => Equipment[ItemSlots.Coat];
    public EquipmentSlot SecondAcc => Equipment[ItemSlots.SecondAcc];

    public void Add(int displaySlot, Item item)
    {
        if (Client == null) return;
        if (displaySlot is <= 0 or > 17) return;
        if (item?.Template == null) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        HandleEquipmentSwap(displaySlot, item);
    }

    private void AddEquipment(int displaySlot, Item item, bool remove = true)
    {
        Equipment[displaySlot] = new EquipmentSlot(displaySlot, item);
        if (remove) RemoveFromInventory(item);
        AddToAislingDb(Client.Aisling, item, displaySlot);
        DisplayToEquipment((byte)displaySlot, item);
        OnEquipmentAdded((byte)displaySlot);
    }

    public void DecreaseDurability()
    {
        var broken = new List<Item>();

        foreach (var item in Equipment.Select(equipment => equipment.Value?.Item).Where(item => item?.Template != null))
        {
            if (item.Template.Flags.FlagIsSet(ItemFlags.Repairable))
            {
                item.Durability--;

                if (item.Durability <= 0)
                    item.Durability = 0;
            }

            ManageDurabilitySignals(item);

            if (item.Durability == 0) broken.Add(item);
            if (item.Durability > item.MaxDurability)
                item.Durability = item.MaxDurability;
        }

        if (broken.Count == 0) return;

        foreach (var item in broken.Where(item => item?.Template != null))
        {
            if (item.ItemQuality != Item.Quality.Damaged)
            {
                item.ItemQuality = Item.Quality.Damaged;
                RemoveFromExisting(item.Slot, true);
                Client.SendMessage(0x02, $"{item.Template.Name} has been damaged.");
            }
            else
            {
                RemoveFromExisting(item.Slot, false);
                Client.SendMessage(0x02, $"{item.Template.Name} was so damaged it fell apart.");
            }
        }
    }

    private void DisplayToEquipment(byte displaySlot, Item item)
    {
        if (item != null)
            Client.Send(new ServerFormat37(item, displaySlot));
    }

    public bool RemoveFromExisting(int displaySlot, bool returnIt = true)
    {
        if (Equipment[displaySlot] == null || displaySlot == 0) return true;

        var itemObj = Equipment[displaySlot].Item;

        if (itemObj == null) return false;

        DeleteFromAislingDb(itemObj);
        RemoveFromSlot(displaySlot);

        if (!returnIt) return HandleUnreturnedItem(itemObj);

        return itemObj.GiveTo(Client.Aisling, false) || HandleUnreturnedItem(itemObj);
    }

    private void HandleEquipmentSwap(int displaySlot, Item item, bool returnIt = true)
    {
        Item itemObj = null;

        if (Equipment[displaySlot] != null)
        {
            itemObj = Equipment[displaySlot].Item;
            DeleteFromAislingDb(itemObj);
        }

        if (item == null) return;

        RemoveFromSlot(displaySlot);
        AddEquipment(displaySlot, item);

        if (!returnIt) HandleUnreturnedItem(itemObj);
        itemObj?.GiveTo(Client.Aisling, false);
    }

    public bool RemoveFromInventory(Item item, bool handleWeight = false)
    {
        if (item != null && Client.Aisling.Inventory.Remove(item.InventorySlot) == null) return true;
        if (item == null) return true;

        Client.Send(new ServerFormat10(item.InventorySlot));

        if (handleWeight)
        {
            Client.Aisling.CurrentWeight -= item.Template.CarryWeight;
            if (Client.Aisling.CurrentWeight < 0)
                Client.Aisling.CurrentWeight = 0;
        }

        Client.LastItemDropped = item;
        Client.SendStats(StatusFlags.StructA);
        item.DeleteFromAislingDb();

        return true;
    }

    private bool HandleUnreturnedItem(Item itemObj)
    {
        if (itemObj == null) return true;

        Client.Aisling.CurrentWeight -= itemObj.Template.CarryWeight;

        if (Client.Aisling.CurrentWeight < 0)
            Client.Aisling.CurrentWeight = 0;

        Client.ObjectHandlers.DelObject(itemObj);
        Client.SendStats(StatusFlags.StructA);
        return true;
    }

    private void ManageDurabilitySignals(Item item)
    {
        if (item.Durability > item.MaxDurability)
            item.MaxDurability = item.Durability;

        var p10 = Math.Abs(item.Durability * 100 / item.MaxDurability);

        if (item.Warnings is not { Length: > 0 }) return;

        switch (p10)
        {
            case <= 10 when !item.Warnings[0]:
                Client.SendMessage(0x03, $"{item.Template.Name} {{=qis almost broken!. Please repair it soon (< 10%)");
                item.Warnings[0] = true;
                break;
            case <= 30 and > 10 when !item.Warnings[1]:
                Client.SendMessage(0x03, $"{item.Template.Name} {{=qis wearing out soon. Please repair it ASAP. (< 30%)");
                item.Warnings[1] = true;
                break;
            case <= 50 and > 30 when !item.Warnings[2]:
                Client.SendMessage(0x03, $"{item.Template.Name} {{=qwill need a repair soon. (< 50%)");
                item.Warnings[2] = true;
                break;
        }
    }

    private void OnEquipmentAdded(byte displaySlot)
    {
        var scripts = Equipment[displaySlot].Item?.Scripts;
        if (scripts != null)
        {
            var scriptValues = scripts.Values;
            foreach (var script in scriptValues)
                script.Equipped(Client.Aisling, displaySlot);
        }

        var item = Equipment[displaySlot].Item;
        if (item != null) item.Equipped = true;

        Client.SendStats(StatusFlags.MultiStat);
        Client.UpdateDisplay();
    }

    private void OnEquipmentRemoved(byte displaySlot)
    {
        if (Equipment[displaySlot] == null) return;

        var scripts = Equipment[displaySlot].Item?.Scripts;
        if (scripts != null)
        {
            var scriptValues = scripts.Values;
            foreach (var script in scriptValues)
                script.UnEquipped(Client.Aisling, displaySlot);
        }

        var item = Equipment[displaySlot].Item;
        if (item != null) item.Equipped = false;

        Client.SendStats(StatusFlags.MultiStat);
        Client.UpdateDisplay();
    }

    private void RemoveFromSlot(int displaySlot)
    {
        OnEquipmentRemoved((byte)displaySlot);
        Client.Aisling.Show(Scope.Self, new ServerFormat38((byte)displaySlot));
        Equipment[displaySlot] = null;
    }

    private async void AddToAislingDb(ISprite aisling, Item item, int slot)
    {
        await CreateLock.WaitAsync().ConfigureAwait(false);

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("ItemToEquipped", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var color = ItemColors.ItemColorsToInt(item.Template.Color);
            var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
            var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
            var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
            var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);

            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = item.Template.Name;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
            cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
            cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
            cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
            cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
            cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
            cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
            cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = slot;
            cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
            cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                aisling.Client.SendMessage(0x03, "Issue equipping gear and saving. Contact GM");
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
            CreateLock.Release();
        }
    }

    private static async void DeleteFromAislingDb(Item item)
    {
        if (item.ItemId == 0) return;

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersEquipped WHERE ItemId = @ItemId";
            await sConn.ExecuteAsync(cmd, new { item.ItemId });
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
}