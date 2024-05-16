using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;
using System.Collections.Concurrent;
using Darkages.Object;
using EquipmentSlot = Darkages.Models.EquipmentSlot;

namespace Darkages.Managers;

public class EquipmentManager
{
    public EquipmentManager(WorldClient client)
    {
        Client = client;
        Equipment = [];

        for (byte i = 1; i < 19; i++)
            Equipment[i] = null;
    }

    public WorldClient Client { get; set; }
    public int Length => Equipment?.Count ?? 0;
    public ConcurrentDictionary<int, EquipmentSlot> Equipment { get; set; }

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
    public EquipmentSlot OverCoat => Equipment[ItemSlots.OverCoat];
    public EquipmentSlot OverHelm => Equipment[ItemSlots.OverHelm];
    public EquipmentSlot SecondAcc => Equipment[ItemSlots.SecondAcc];
    public EquipmentSlot ThirdAcc => Equipment[ItemSlots.ThirdAcc];

    public void Add(int displaySlot, Item item)
    {
        if (Client == null) return;
        if (displaySlot is <= 0 or > 18) return;
        if (item?.Template == null) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        HandleEquipmentSwap(displaySlot, item);
    }

    /// <summary>
    /// Made public for adding items directly on character creation
    /// </summary>
    private void AddEquipment(int displaySlot, Item item, bool remove = true)
    {
        Equipment[displaySlot] = new EquipmentSlot(displaySlot, item);
        if (remove) RemoveFromInventoryToEquip(item);
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
                if (item.Durability > 0)
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
            item.ItemQuality = Item.Quality.Damaged;
            RemoveFromExisting(item.Slot);
            Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} has been damaged.");
        }
    }

    private void DisplayToEquipment(byte displaySlot, Item item)
    {
        if (item != null)
            Client.SendEquipment(displaySlot, item);
    }

    public bool RemoveFromExisting(int displaySlot)
    {
        if (Equipment[displaySlot] == null || displaySlot == 0) return true;
        var itemObj = Equipment[displaySlot].Item;
        if (itemObj == null) return false;

        RemoveFromSlot(displaySlot);
        return itemObj.GiveTo(Client.Aisling, false) || HandleUnreturnedItem(itemObj);
    }

    private void HandleEquipmentSwap(int displaySlot, Item item)
    {
        Item itemObj = null;

        if (Equipment[displaySlot] != null)
        {
            itemObj = Equipment[displaySlot].Item;
        }

        if (item == null) return;

        RemoveFromSlot(displaySlot);
        AddEquipment(displaySlot, item);

        if (itemObj == null) return;
        var givenBack = itemObj.GiveTo(Client.Aisling, false);
        if (givenBack) return;
        Client.Aisling.BankManager.Items.TryAdd(itemObj.ItemId, itemObj);
        Client.SendServerMessage(ServerMessageType.ActiveMessage, "Inventory full, item deposited to bank");
    }

    private void RemoveFromInventoryToEquip(Item item, bool handleWeight = false)
    {
        if (item == null) return;

        if (Client.Aisling.Inventory.Items.TryUpdate(item.InventorySlot, null, item))
            Client.SendRemoveItemFromPane(item.InventorySlot);

        if (handleWeight)
        {
            Client.Aisling.CurrentWeight -= item.Template.CarryWeight;
            if (Client.Aisling.CurrentWeight < 0)
                Client.Aisling.CurrentWeight = 0;
        }

        Client.SendAttributes(StatUpdateType.WeightGold);
    }

    private bool HandleUnreturnedItem(Item itemObj)
    {
        if (itemObj == null) return true;

        Client.Aisling.CurrentWeight -= itemObj.Template.CarryWeight;

        if (Client.Aisling.CurrentWeight < 0)
            Client.Aisling.CurrentWeight = 0;

        Client.Aisling.BankManager.Items.TryAdd(itemObj.ItemId, itemObj);
        Client.SendAttributes(StatUpdateType.WeightGold);
        return true;
    }

    private void ManageDurabilitySignals(Item item)
    {
        if (item.Durability > item.MaxDurability)
            item.MaxDurability = item.Durability;

        var p10 = item.Durability * 100 / item.MaxDurability;

        if (item.Warnings is not { Length: > 0 }) return;

        switch (p10)
        {
            case <= 10 when !item.Warnings[0]:
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis almost broken!. Please repair it soon (< 10%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis almost broken!. Please repair it soon (< 10%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis almost broken!. Please repair it soon (< 10%)");
                item.Warnings[0] = true;
                break;
            case <= 30 and > 10 when !item.Warnings[1]:
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis wearing out soon. Please repair it ASAP. (< 30%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis wearing out soon. Please repair it ASAP. (< 30%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qis wearing out soon. Please repair it ASAP. (< 30%)");
                item.Warnings[1] = true;
                break;
            case <= 50 and > 30 when !item.Warnings[2]:
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qwill need a repair soon. (< 50%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qwill need a repair soon. (< 50%)");
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{item.Template.Name} {{=qwill need a repair soon. (< 50%)");
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
        if (item != null)
        {
            item.ItemPane = Item.ItemPanes.Equip;
            item.ReapplyItemModifiers(Client);
        }

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
        if (item != null) item.ItemPane = Item.ItemPanes.Inventory;

        Client.UpdateDisplay();
    }

    private void RemoveFromSlot(int displaySlot)
    {
        OnEquipmentRemoved((byte)displaySlot);
        Client.SendUnequip((Chaos.Common.Definitions.EquipmentSlot)displaySlot);
        Equipment.TryUpdate(displaySlot, null, FindInSlot(displaySlot));
        var item = new Item();
        item.ReapplyItemModifiers(Client);
    }

    private EquipmentSlot FindInSlot(int slot)
    {
        EquipmentSlot ret = null;

        if (Equipment.TryGetValue(slot, out var item))
            ret = item;

        return ret?.Item is { Template: not null } ? ret : null;
    }
}