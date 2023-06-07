using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.GameScripts.Mundanes.Generic;

public static class ShopMethods
{
    public static Item ItemDetail { get; set; }

    public static IEnumerable<ItemTemplate> BuyFromStoreInventory(Mundane mundane)
    {
        var defaultBag = mundane.Template.DefaultMerchantStock.Select(i =>
            ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(i, out var value)
                ? value
                : null);

        return ServerSetup.Instance.GlobalItemTemplateCache.Values.Where(i => i.NpcKey == mundane.Template.Name)
            .ToList().Concat(defaultBag.Where(n => n != null));
    }

    public static IEnumerable<byte> GetCharacterSellInventoryByteList(GameClient client)
    {
        return client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.Flags.FlagIsSet(ItemFlags.Sellable)).ToList()
            .Select(i => i.InventorySlot).ToList();
    }

    public static void CompletePendingItemSell(GameClient client)
    {
        if (ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(client.PendingItemSessions.Name))
        {
            var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

            if (item != null)
                if (client.Aisling.GiveGold(client.PendingItemSessions.Offer))
                {
                    client.Aisling.Inventory.RemoveRange(client, item,
                        client.PendingItemSessions.Removing);
                    client.PendingItemSessions = null;
                    client.SendStats(StatusFlags.WeightMoney);
                    return;
                }
        }

        client.PendingItemSessions = null;
        client.SendStats(StatusFlags.WeightMoney);
    }

    public static List<byte> GetCharacterDetailingByteListForLowGradePolish(GameClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && (i.Template.CanStack == false && i.Template.Enchantable)));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common).Select(i => i.InventorySlot).ToList();
    }

    public static List<byte> GetCharacterDetailingByteListForMidGradePolish(GameClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && (i.Template.CanStack == false && i.Template.Enchantable)));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common or Item.Quality.Uncommon or Item.Quality.Rare).Select(i => i.InventorySlot).ToList();
    }

    public static List<byte> GetCharacterDetailingByteListForHighGradePolish(GameClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && (i.Template.CanStack == false && i.Template.Enchantable)));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common or Item.Quality.Uncommon or Item.Quality.Rare or Item.Quality.Epic or Item.Quality.Legendary).Select(i => i.InventorySlot).ToList();
    }

    public static long GetRepairCosts(GameClient client)
    {
        long repairCosts = 0;

        repairCosts += client.Aisling.Inventory.Items.Where(i => i.Value != null && i.Value.Template.Flags.FlagIsSet(ItemFlags.Repairable)
                && i.Value.Durability < i.Value.MaxDurability)
            .Sum(i => i.Value.Template.Value / 4);

        repairCosts += client.Aisling.EquipmentManager.Equipment.Where(equip => equip.Value != null
            && equip.Value.Item.Template.Flags.FlagIsSet(ItemFlags.Repairable)
            && equip.Value.Item.Durability < equip.Value.Item.MaxDurability).Aggregate(repairCosts, (current, equip) => current + equip.Value.Item.Template.Value / 4);

        return repairCosts;
    }

    public static uint GetDetailCosts(GameClient client, string args)
    {
        ItemDetail = client.Aisling.Inventory.Get(i => i != null && i.InventorySlot == Convert.ToInt32(args)).FirstOrDefault();

        if (ItemDetail == null) return 0;

        return ItemDetail.OriginalQuality switch
        {
            Item.Quality.Damaged => 1000,
            Item.Quality.Common => 20000,
            Item.Quality.Uncommon => 50000,
            Item.Quality.Rare => 100000,
            Item.Quality.Epic => 1000000,
            Item.Quality.Legendary => 25000000,
            Item.Quality.Forsaken => 100000000,
            Item.Quality.Mythic => 1000000000,
            _ => 0
        };
    }

    public static Item.Quality DungeonLowQuality()
    {
        var qualityGen = Generator.RandomNumPercentGen();

        return qualityGen switch
        {
            >= 0 and <= .40 => Item.Quality.Common,
            > .40 and <= .99 => Item.Quality.Uncommon,
            > .99 and <= 1 => Item.Quality.Rare,
            _ => Item.Quality.Damaged
        };
    }

    public static Item.Quality DungeonMediumQuality()
    {
        var qualityGen = Generator.RandomNumPercentGen();

        return qualityGen switch
        {
            >= 0 and <= .20 => Item.Quality.Common,
            > .20 and <= .75 => Item.Quality.Uncommon,
            > .75 and <= .90 => Item.Quality.Rare,
            > .90 and <= 1 => Item.Quality.Epic,
            _ => Item.Quality.Damaged
        };
    }

    public static Item.Quality DungeonHighQuality()
    {
        var qualityGen = Generator.RandomNumPercentGen();

        return qualityGen switch
        {
            >= 0 and <= .50 => Item.Quality.Uncommon,
            > .50 and <= .88 => Item.Quality.Rare,
            > .88 and <= .98 => Item.Quality.Epic,
            > .98 and <= .9975 => Item.Quality.Legendary,
            > .9975 and <= 1 => Item.Quality.Forsaken,
            _ => Item.Quality.Damaged
        };
    }
}