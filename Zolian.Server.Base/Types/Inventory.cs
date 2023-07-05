using System.Collections.Concurrent;
using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Types;

public class Inventory : ObjectManager, IInventory
{
    private const int Length = 59;

    public bool IsFull => TotalItems >= Length;

    public readonly ConcurrentDictionary<int, Item> Items = new();

    public Inventory()
    {
        for (var i = 0; i < Length; i++) Items[i + 1] = null;
    }

    public IEnumerable<byte> BankList => (Items.Where(i => i.Value is {Template: not null } && i.Value.Template.Flags.FlagIsSet(ItemFlags.Bankable))).Select(i => i.Value.InventorySlot);

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

    public void Remove(WorldClient client, Item item)
    {
        if (item == null)
            return;

        if (Remove(item.InventorySlot) != null)
            client.SendRemoveItemFromPane(item.InventorySlot);
        client.SendAttributes(StatUpdateType.Primary);
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

    public void RemoveFromInventory(WorldClient client, Item item)
    {
        if (item != null && client.Aisling.Inventory.Remove(item.InventorySlot) == null) return;
        if (item == null) return;

        client.SendRemoveItemFromPane(item.InventorySlot);
        client.LastItemDropped = item;
        client.SendAttributes(StatUpdateType.Primary);
        item.DeleteFromAislingDb();
    }

    public void RemoveRange(WorldClient client, Item item, int range)
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
            client.SendRemoveItemFromPane(item.InventorySlot);
            client.Aisling.Inventory.Set(item);
            UpdateSlot(client, item);
        }

        UpdatePlayersWeight(client);
    }

    public void AddRange(WorldClient client, Item item, int range)
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
            client.SendRemoveItemFromPane(item.InventorySlot);
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

    public void UpdateSlot(WorldClient client, Item item)
    {
        item.Scripts = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);
        if (!string.IsNullOrEmpty(item.Template.WeaponScript))
            item.WeaponScripts = ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);
        client.SendAddItemToPane(item);
        UpdatePlayersWeight(client);
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

    public override bool Equals(object obj)
    {
        if (obj is not Inventory inv) return false;
        return Equals(Items.Values, inv.Items.Values) && Equals(Items.Keys, inv.Items.Keys);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + Items.Values.GetHashCode();
            return hash * 23 + Items.Keys.GetHashCode();
        }
    }
}

public class InventoryComparer : IEqualityComparer<Inventory>
{
    public bool Equals(Inventory x, Inventory y)
    {
        if (x == null || y == null) return false;
        if (ReferenceEquals(x, y)) return true;
        return Equals(x.Items.Values, y.Items.Values) && Equals(x.Items.Keys, y.Items.Keys);
    }

    public int GetHashCode(Inventory obj)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + obj.Items.Values.GetHashCode();
            return hash * 23 + obj.Items.Keys.GetHashCode();
        }
    }
}