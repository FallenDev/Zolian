using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Common;

public static class NpcShopExtensions
{
    public static Item ItemDetail { get; private set; }

    #region Buy

    /// <summary>
    /// Takes the default list of strings containing item names. It compares that list against the Global Item Cache for matches
    /// Looks to see if items specify a Npc, if they do, and they match the current Npc. It then adds them to the default list
    /// </summary>
    /// <returns>List of ItemTemplates</returns>
    public static IEnumerable<ItemTemplate> BuyFromStoreInventory(Mundane mundane)
    {
        var defaultBag = mundane.Template.DefaultMerchantStock.Select(i =>
            ServerSetup.Instance.GlobalItemTemplateCache.GetValueOrDefault(i));

        // NpcKey is generally blank in the database, it gives another way to declare a Npc can carry an item
        return ServerSetup.Instance.GlobalItemTemplateCache.Values.Where(i => i.NpcKey == mundane.Template.Name)
            .ToList().Concat(defaultBag.Where(n => n != null));
    }

    public static void BuyItemFromInventory(WorldClient client, Mundane mundane, string args)
    {
        ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(args, out var itemTemplate);
        if (itemTemplate == null)
        {
            client.SendOptionsDialog(mundane, "I'm sorry, freshly sold out.");
            return;
        }

        client.PendingBuySessions = new PendingBuy
        {
            Name = itemTemplate.Name,
            Offer = (int)itemTemplate.Value,
            Quantity = 1
        };

        if (itemTemplate.CanStack)
            client.SendTextInput(mundane, "How many would you like?", "Amount:");
        else
        {
            var itemName = client.PendingBuySessions.Name;

            if (itemName == null)
            {
                client.SendOptionsDialog(mundane, "I'm sorry, freshly sold out.");
                return;
            }

            var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
            var opts = new List<Dialog.OptionsDataItem>
            {
                new(0x19, ServerSetup.Instance.Config.MerchantConfirmMessage),
                new(0x20, ServerSetup.Instance.Config.MerchantCancelMessage)
            };

            client.SendOptionsDialog(mundane, $"It will cost you a total of {cost} coins for {{=c1 {{=q{itemName}s{{=a. Is that a deal?", opts.ToArray());
        }
    }

    public static void BuyStackedItemFromInventory(WorldClient client, Mundane mundane)
    {
        var itemName = client.PendingBuySessions.Name;
        var amount = client.PendingBuySessions.Quantity;

        if (itemName == null)
        {
            client.SendOptionsDialog(mundane, "I'm sorry, freshly sold out.");
            return;
        }

        var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
        var opts = new List<Dialog.OptionsDataItem>
        {
            new(0x19, ServerSetup.Instance.Config.MerchantConfirmMessage),
            new(0x20, ServerSetup.Instance.Config.MerchantCancelMessage)
        };

        client.SendOptionsDialog(mundane, $"It will cost you a total of {cost} coins for {{=c{amount} {{=q{itemName}s{{=a. Is that a deal?", opts.ToArray());
    }

    #endregion

    #region Sell

    public static IEnumerable<byte> GetCharacterSellInventoryByteList(WorldClient client)
    {
        return client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.Flags.FlagIsSet(ItemFlags.Sellable)).ToList()
            .Select(i => i.InventorySlot).ToList();
    }

    public static void CompletePendingItemSell(WorldClient client, Mundane mundane)
    {
        var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).FirstOrDefault();

        if (item == null)
        {
            client.SendOptionsDialog(mundane, "Sorry, it looks like you're not carrying what you offered.");
            return;
        }

        var offer = item.Template.Value / 2;
        if (offer <= 0) return;

        if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
        {
            client.Aisling.GoldPoints += offer;
            client.Aisling.Inventory.RemoveFromInventory(client, item);
            client.SendAttributes(StatUpdateType.Primary);
            client.SendAttributes(StatUpdateType.ExpGold);
            client.PendingItemSessions = null;
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
        }
        else
        {
            client.PendingItemSessions = null;
            client.SendOptionsDialog(mundane, "Sorry, it looks like you won't be able to carry the gold.");
        }
    }

    public static void SellItemFromInventory(WorldClient client, Mundane mundane, string args)
    {
        if (client.PendingItemSessions != null)
        {
            var successful = ushort.TryParse(args, out var quantity);
            if (!successful) return;
            client.PendingItemSessions.Quantity = quantity;
            SellStackedItemFromInventory(client, mundane);
            return;
        }

        client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var itemFromSlot);

        if (itemFromSlot == null)
        {
            client.SendOptionsDialog(mundane, "Sorry, what was that again?");
            return;
        }

        var offer = (int)(itemFromSlot.Template.Value / 2);

        if (offer <= 0)
        {
            client.SendOptionsDialog(mundane, ServerSetup.Instance.Config.MerchantRefuseTradeMessage);
            return;
        }

        if (itemFromSlot.Stacks > 1 && itemFromSlot.Template.CanStack)
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = itemFromSlot.ItemId,
                Name = itemFromSlot.Template.Name,
                Quantity = 0
            };

            client.SendTextInput(mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}");
        }
        else
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = itemFromSlot.ItemId,
                Name = itemFromSlot.Template.Name,
                Quantity = 1
            };

            var opts2 = new List<Dialog.OptionsDataItem>
            {
                new(0x30, ServerSetup.Instance.Config.MerchantConfirmMessage),
                new(0x20, ServerSetup.Instance.Config.MerchantCancelMessage)
            };

            client.SendOptionsDialog(mundane,
                $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)",
                itemFromSlot.Template.Name, opts2.ToArray());
        }
    }

    public static void SellStackedItemFromInventory(WorldClient client, Mundane mundane)
    {
        var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).FirstOrDefault();
        var amount = client.PendingItemSessions.Quantity;

        if (item == null || item.Stacks < amount)
        {
            client.SendOptionsDialog(mundane, "Sorry, it looks like you're not carrying enough for what you offered.");
            return;
        }

        var offer = item.Template.Value / 2;
        offer *= amount;

        if (offer <= 0) return;

        if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
        {
            client.Aisling.GoldPoints += offer;

            if (item.Stacks == amount)
                client.Aisling.Inventory.RemoveFromInventory(client, item);
            else
                client.Aisling.Inventory.RemoveRange(client, item, amount);

            client.SendAttributes(StatUpdateType.Primary);
            client.SendAttributes(StatUpdateType.ExpGold);
            client.PendingItemSessions = null;
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
        }
        else
        {
            client.PendingItemSessions = null;
            client.SendOptionsDialog(mundane, "Sorry, it looks like you won't be able to carry the gold.");
        }
    }

    public static void SellItemDroppedFromInventory(WorldClient client, Mundane mundane, string args)
    {
        client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var itemFromSlot);

        if (itemFromSlot == null) return;
        var offer = (int)(itemFromSlot.Template.Value / 2);

        if (offer <= 0)
        {
            client.SendOptionsDialog(mundane, ServerSetup.Instance.Config.MerchantRefuseTradeMessage);
            return;
        }

        if (itemFromSlot.Stacks > 1 && itemFromSlot.Template.CanStack)
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = itemFromSlot.ItemId,
                Name = itemFromSlot.Template.Name,
                Quantity = 0
            };

            client.SendTextInput(mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}", "Amount:", 3);
        }
        else
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = itemFromSlot.ItemId,
                Name = itemFromSlot.Template.Name,
                Quantity = 1
            };

            var opts2 = new List<Dialog.OptionsDataItem>
            {
                new(0x19, ServerSetup.Instance.Config.MerchantConfirmMessage),
                new(0x20, ServerSetup.Instance.Config.MerchantCancelMessage)
            };

            client.SendOptionsDialog(mundane,
                $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)",
                itemFromSlot.Template.Name, opts2.ToArray());
        }
    }

    public static void AutoSellItemDroppedFromInventory(WorldClient client, Mundane mundane, string args)
    {
        client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var itemFromSlot);

        if (itemFromSlot == null)
        {
            client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Well? Where is it?");
            return;
        }

        var offer = itemFromSlot.Template.Value / 2;

        if (offer <= 0)
        {
            client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: I can't accept that.");
            return;
        }

        if (itemFromSlot.Stacks > 1 && itemFromSlot.Template.CanStack)
        {
            client.PendingItemSessions = new PendingSell
            {
                ID = itemFromSlot.ItemId,
                Name = itemFromSlot.Template.Name,
                Quantity = 0
            };

            client.SendTextInput(mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}", "Amount:", 3);
        }
        else
        {
            if (offer > itemFromSlot.Template.Value) return;

            if (client.Aisling.GoldPoints + offer > ServerSetup.Instance.Config.MaxCarryGold)
            {
                client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Your gold satchel is full!");
                return;
            }

            client.Aisling.GoldPoints += offer;
            client.Aisling.Inventory.RemoveFromInventory(client, itemFromSlot);
            client.SendAttributes(StatUpdateType.WeightGold);
            client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Thank you!");
        }
    }

    #endregion

    #region Bank

    /// <summary>
    /// Looks through player's bankmanager for items and returns them
    /// </summary>
    /// <returns>List of ItemTemplates</returns>
    public static IEnumerable<Item> WithdrawFromBank(WorldClient client)
    {
        return client.Aisling.BankManager.Items.Values.Where(i => i != null && !i.Template.CanStack);
    }

    /// <summary>
    /// Looks through player's bankmanager for items and returns them
    /// </summary>
    /// <returns>List of ItemTemplates</returns>
    public static IEnumerable<Item> WithdrawStackedFromBank(WorldClient client)
    {
        return client.Aisling.BankManager.Items.Values.Where(i => i != null && i.Template.CanStack);
    }

    /// <summary>
    /// Returns a list of items from your inventory that are "Bankable"
    /// </summary>
    /// <returns>IEnumerable byte array</returns>
    public static IEnumerable<byte> GetCharacterBankInventoryByteList(WorldClient client)
    {
        return client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.Flags.FlagIsSet(ItemFlags.Bankable)).ToList()
            .Select(i => i.InventorySlot).ToList();
    }

    #endregion

    #region Item Quality Setters

    /// <summary>
    /// Sets the quality between Common -> Rare
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality DungeonLowQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .40 => Item.Quality.Common,
            > .40 and <= .99 => Item.Quality.Uncommon,
            > .99 => Item.Quality.Rare,
            _ => Item.Quality.Damaged
        };
    }

    /// <summary>
    /// Sets the quality between Common -> Epic
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality DungeonMediumQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .75 => Item.Quality.Uncommon,
            > .75 and <= .99 => Item.Quality.Rare,
            > .99 => Item.Quality.Epic,
            _ => Item.Quality.Damaged
        };
    }

    /// <summary>
    /// Sets the quality between Uncommon -> Forsaken
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality DungeonHighQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .88 => Item.Quality.Rare,
            > .88 and <= .98 => Item.Quality.Epic,
            > .98 and <= .9975 => Item.Quality.Legendary,
            > .9975 => Item.Quality.Forsaken,
            _ => Item.Quality.Damaged
        };
    }

    /// <summary>
    /// Sets the quality between Uncommon -> Epic
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality QuestLowQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .75 => Item.Quality.Uncommon,
            >= .75 and <= .95 => Item.Quality.Rare,
            > .95 => Item.Quality.Epic,
            _ => Item.Quality.Damaged
        };
    }

    /// <summary>
    /// Sets the quality between Rare -> Legendary
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality QuestMediumQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .75 => Item.Quality.Rare,
            >= .75 and <= .90 => Item.Quality.Epic,
            > .90 => Item.Quality.Legendary,
            _ => Item.Quality.Damaged
        };
    }

    /// <summary>
    /// Sets the quality between Epic -> Forsaken
    /// </summary>
    /// <returns>Quality</returns>
    public static Item.Quality QuestHighQuality()
    {
        var qualityGen = Generator.RandomPercentPrecise();

        return qualityGen switch
        {
            >= 0 and <= .75 => Item.Quality.Epic,
            >= .75 and <= .90 => Item.Quality.Legendary,
            > .90 => Item.Quality.Forsaken,
            _ => Item.Quality.Damaged
        };
    }

    #endregion

    #region Blacksmithing & Armorsmithing

    // Increase weapons to +1
    public static List<byte> GetCharacterNoviceWeaponImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable && i.Template.EquipmentSlot == 1));

        return inventory.Where(w => w.GearEnhancement == Item.GearEnhancements.None).Select(i => i.InventorySlot).ToList();
    }

    // Increase weapons to +2
    public static List<byte> GetCharacterApprenticeWeaponImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable && i.Template.EquipmentSlot == 1));

        return inventory.Where(w => w.GearEnhancement is Item.GearEnhancements.None or Item.GearEnhancements.One)
            .Select(i => i.InventorySlot).ToList();
    }

    // Increase weapons to +3
    public static List<byte> GetCharacterJourneymanWeaponImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable && i.Template.EquipmentSlot == 1));

        return inventory.Where(w => w.GearEnhancement is Item.GearEnhancements.None or Item.GearEnhancements.One or Item.GearEnhancements.Two)
            .Select(i => i.InventorySlot).ToList();
    }

    // Increase weapons to +4
    public static List<byte> GetCharacterExpertWeaponImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable && i.Template.EquipmentSlot == 1));

        return inventory.Where(w => w.GearEnhancement is Item.GearEnhancements.None or Item.GearEnhancements.One or Item.GearEnhancements.Two or Item.GearEnhancements.Three)
            .Select(i => i.InventorySlot).ToList();
    }

    // Increase weapons to +6
    public static List<byte> GetCharacterArtisanWeaponImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable && i.Template.EquipmentSlot == 1));

        return inventory.Where(w => w.GearEnhancement is Item.GearEnhancements.None or Item.GearEnhancements.One or Item.GearEnhancements.Two or Item.GearEnhancements.Three or Item.GearEnhancements.Four or Item.GearEnhancements.Five)
            .Select(i => i.InventorySlot).ToList();
    }

    public static uint GetSmithingCosts(WorldClient client, string args)
    {
        ItemDetail = client.Aisling.Inventory.Get(i => i != null && i.InventorySlot == Convert.ToInt32(args)).FirstOrDefault();

        if (ItemDetail == null) return 0;

        return ItemDetail.OriginalQuality switch
        {
            Item.Quality.Damaged => 1000,
            Item.Quality.Common => 5000,
            Item.Quality.Uncommon => 7500,
            Item.Quality.Rare => 15000,
            Item.Quality.Epic => 30000,
            Item.Quality.Legendary => 50000,
            Item.Quality.Forsaken => 100000,
            Item.Quality.Mythic => 300000,
            _ => 0
        };
    }

    // Increase armor None -> Iron
    public static List<byte> GetCharacterNoviceArmorImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.EquipmentSlot == 2));
        return inventory.Where(w => w.ItemMaterial is Item.ItemMaterials.None or Item.ItemMaterials.Copper).Select(i => i.InventorySlot).ToList();
    }

    // Increase armor Steel -> Elven
    public static List<byte> GetCharacterApprenticeArmorImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.EquipmentSlot == 2));
        return inventory.Where(w => w.ItemMaterial is Item.ItemMaterials.None or Item.ItemMaterials.Copper or Item.ItemMaterials.Iron
            or Item.ItemMaterials.Steel or Item.ItemMaterials.Forged).Select(i => i.InventorySlot).ToList();
    }

    // Increase armor Dwarven -> Hybrasyl
    public static List<byte> GetCharacterJourneymanArmorImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.EquipmentSlot == 2));
        return inventory.Where(w => w.ItemMaterial is Item.ItemMaterials.None or Item.ItemMaterials.Copper or Item.ItemMaterials.Iron
            or Item.ItemMaterials.Steel or Item.ItemMaterials.Forged or Item.ItemMaterials.Elven
            or Item.ItemMaterials.Dwarven or Item.ItemMaterials.Mythril).Select(i => i.InventorySlot).ToList();
    }

    // Increase armor MoonStone -> Runic
    public static List<byte> GetCharacterExpertArmorImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.EquipmentSlot == 2));
        return inventory.Where(w => w.ItemMaterial is Item.ItemMaterials.None or Item.ItemMaterials.Copper or Item.ItemMaterials.Iron
            or Item.ItemMaterials.Steel or Item.ItemMaterials.Forged or Item.ItemMaterials.Elven
            or Item.ItemMaterials.Dwarven or Item.ItemMaterials.Mythril or Item.ItemMaterials.Hybrasyl
            or Item.ItemMaterials.MoonStone or Item.ItemMaterials.SunStone or Item.ItemMaterials.Ebony).Select(i => i.InventorySlot).ToList();
    }

    // Increase armor Chaos
    public static List<byte> GetCharacterArtisanalArmorImprove(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.EquipmentSlot == 2));
        return inventory.Where(w => w.ItemMaterial is Item.ItemMaterials.None or Item.ItemMaterials.Copper or Item.ItemMaterials.Iron
            or Item.ItemMaterials.Steel or Item.ItemMaterials.Forged or Item.ItemMaterials.Elven
            or Item.ItemMaterials.Dwarven or Item.ItemMaterials.Mythril or Item.ItemMaterials.Hybrasyl
            or Item.ItemMaterials.MoonStone or Item.ItemMaterials.SunStone or Item.ItemMaterials.Ebony
            or Item.ItemMaterials.Runic).Select(i => i.InventorySlot).ToList();
    }

    public static uint GetArmorSmithingCosts(WorldClient client, string args)
    {
        ItemDetail = client.Aisling.Inventory.Get(i => i != null && i.InventorySlot == Convert.ToInt32(args)).FirstOrDefault();

        if (ItemDetail == null) return 0;

        return ItemDetail.ItemMaterial switch
        {
            Item.ItemMaterials.None => 1000,
            Item.ItemMaterials.Copper => 5000,
            Item.ItemMaterials.Iron => 15000,
            Item.ItemMaterials.Steel => 59000,
            Item.ItemMaterials.Forged => 210000,
            Item.ItemMaterials.Elven => 300000,
            Item.ItemMaterials.Dwarven => 570000,
            Item.ItemMaterials.Mythril => 900000,
            Item.ItemMaterials.Hybrasyl => 1800000,
            Item.ItemMaterials.MoonStone => 12000000,
            Item.ItemMaterials.SunStone => 20000000,
            Item.ItemMaterials.Ebony => 43000000,
            Item.ItemMaterials.Runic => 200000000,
            Item.ItemMaterials.Chaos => 500000000,
            _ => 0
        };
    }

    #endregion

    #region Repair

    public static List<byte> GetCharacterDetailingByteListForLowGradePolish(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common).Select(i => i.InventorySlot).ToList();
    }

    public static List<byte> GetCharacterDetailingByteListForMidGradePolish(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common or Item.Quality.Uncommon).Select(i => i.InventorySlot).ToList();
    }

    public static List<byte> GetCharacterDetailingByteListForHighGradePolish(WorldClient client)
    {
        var inventory = new List<Item>(client.Aisling.Inventory.Items.Values.Where(i =>
            i != null && i.Template.CanStack == false && i.Template.Enchantable));

        return inventory.Where(i => i.ItemQuality is Item.Quality.Damaged or Item.Quality.Common or Item.Quality.Uncommon or Item.Quality.Rare or Item.Quality.Epic or Item.Quality.Legendary).Select(i => i.InventorySlot).ToList();
    }

    public static long GetRepairCosts(WorldClient client)
    {
        double repairCosts = 0;

        foreach (var inventory in client.Aisling.Inventory.Items.Where(i => i.Value != null && i.Value.Template.Flags.FlagIsSet(ItemFlags.Repairable) && i.Value.Durability < i.Value.MaxDurability))
        {
            var item = inventory.Value;
            if (item.Template == null) continue;
            var tempValue = Math.Abs(item.Durability / (double)item.MaxDurability - 1);
            repairCosts += item.Template.Value * tempValue;
        }

        foreach (var (key, value) in client.Aisling.EquipmentManager.Equipment.Where(equip => equip.Value != null && equip.Value.Item.Template.Flags.FlagIsSet(ItemFlags.Repairable) && equip.Value.Item.Durability < equip.Value.Item.MaxDurability))
        {
            var item = value.Item;
            if (item.Template == null) continue;
            var tempValue = Math.Abs(item.Durability / (double)item.MaxDurability - 1);
            repairCosts += item.Template.Value * tempValue;
        }

        return (long)repairCosts;
    }

    public static uint GetDetailCosts(WorldClient client, string args)
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

    #endregion

    public static IEnumerable<byte> GetCharacterGiftBoxInventoryByteList(WorldClient client)
    {
        return client.Aisling.Inventory.Items.Values.Where(i => i != null && i.Template.Flags.FlagIsSet(ItemFlags.Dropable) && !i.Template.CanStack).ToList()
            .Select(i => i.InventorySlot).ToList();
    }
}