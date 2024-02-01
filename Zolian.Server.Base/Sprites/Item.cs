using Chaos.Common.Definitions;
using Chaos.Common.Identity;

using Dapper;

using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.Sprites;

public sealed class Item : Sprite, IItem
{
    public enum ItemPanes
    {
        Ground,
        Inventory,
        Equip,
        Bank,
        Archived
    }

    public enum Quality
    {
        Damaged,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Forsaken,
        Mythic,
        Primordial,
        Transcendent
    }

    public enum Variance
    {
        None,
        Embunement,
        Blessing,
        Mana,
        Gramail,
        Deoch,
        Ceannlaidir,
        Cail,
        Fiosachd,
        Glioca,
        Luathas,
        Sgrios,
        Reinforcement,
        Spikes
    }

    public enum WeaponVariance
    {
        None,
        Bleeding,
        Rending,
        Aegis,
        Reaping,
        Vampirism,
        Ghosting,
        Haste,
        Gust,
        Quake,
        Rain,
        Flame,
        Dusk,
        Dawn
    }

    /// <summary>
    /// Each tier is 5% boost to min/max dmg
    /// Tier 3 gives +2 all stats 1k hp/mp
    /// Tier 5 gives +3 all stats 2k hp/mp
    /// Tier 6 gives +4 all stats 3k hp/mp
    /// </summary>
    public enum GearEnhancements
    {
        None,
        One,
        Two,
        Three,
        Four,
        Five,
        Six
    }

    /// <summary>
    /// Each tier is +1 AC, 5 MR, 5 HIT
    /// Tier 3 (Steel) +1 all stats 500 hp/mp
    /// Tier 5 (Elven) +2 all stats 750 hp/mp
    /// Tier 7 (Mythril) +2 all stats 2500 hp/mp
    /// Tier 9 (MoonStone) +3 all stats 3000 hp/mp
    /// Tier 10 (SunStone) +3 all stats 5000 hp/mp
    /// Tier 11 (Ebony) +4 all stats 6000 hp/mp
    /// Tier 12 (Runic) +5 all stats 10000 hp/mp
    /// Tier 13 (Chaos) +5 all stats 20000 hp/mp
    /// </summary>
    public enum ItemMaterials
    {
        None,
        Copper,
        Iron,
        Steel,
        Forged,
        Elven,
        Dwarven,
        Mythril,
        Hybrasyl,
        MoonStone,
        SunStone,
        Ebony,
        Runic,
        Chaos
    }

    public long ItemId { get; set; }
    public ItemTemplate Template { get; set; }
    public string Name { get; set; }
    public string DisplayName => GetDisplayName();
    public string NoColorDisplayName => NoColorGetDisplayName();
    public ItemPanes ItemPane { get; set; }
    public byte Slot { get; set; }
    public byte InventorySlot { get; set; }
    public byte Color { get; set; }
    public bool Cursed { get; set; }
    public uint Durability { get; set; }
    public uint MaxDurability { get; set; }
    public bool Identified { get; init; }
    public Variance ItemVariance { get; set; }
    public WeaponVariance WeapVariance { get; set; }
    public Quality ItemQuality { get; set; }
    public Quality OriginalQuality { get; set; }
    public ushort Stacks { get; set; }
    public bool Enchantable { get; set; }
    public bool Tarnished { get; set; }
    public GearEnhancements GearEnhancement { get; set; }
    public ItemMaterials ItemMaterial { get; set; }
    public ConcurrentDictionary<string, ItemScript> Scripts { get; set; }
    public ConcurrentDictionary<string, WeaponScript> WeaponScripts { get; set; }
    public int Dropping { get; set; }
    public bool[] Warnings { get; set; }
    public ushort Image { get; init; }
    public ushort DisplayImage { get; init; }

    public string GetDisplayName()
    {
        var colorCode = ItemQuality switch
        {
            Quality.Damaged => "{=jDamaged ",
            Quality.Common => "",
            Quality.Uncommon => "{=qUncommon ",
            Quality.Rare => "{=cRare ",
            Quality.Epic => "{=pEpic ",
            Quality.Legendary => "{=sLegendary ",
            Quality.Forsaken => "{=bForsaken ",
            Quality.Mythic => "{=fMythic ",
            Quality.Primordial => "{=dPrimordial ",
            Quality.Transcendent => "{=oTranscendent ",
            _ => ""
        };

        var enhanceCode = GearEnhancement switch
        {
            GearEnhancements.None => "",
            GearEnhancements.One => " +1",
            GearEnhancements.Two => " +2",
            GearEnhancements.Three => " +3",
            GearEnhancements.Four => " +4",
            GearEnhancements.Five => " +5",
            GearEnhancements.Six => " +6",
            _ => ""
        };

        if (Tarnished)
            colorCode = "{=jTarnished ";

        if (!Enchantable) return Template.Name;
        if (ItemVariance != Variance.None && ItemQuality != Quality.Common)
        {
            var displayName = colorCode + Template.Name + " of " + ItemVariance;
            return displayName;
        }

        if (ItemVariance != Variance.None && ItemQuality == Quality.Common)
        {
            var displayName = Template.Name + " of " + ItemVariance;
            return displayName;
        }

        if (WeapVariance != WeaponVariance.None && ItemQuality != Quality.Common)
        {
            var displayName = colorCode + Template.Name + " of " + WeapVariance;

            if (GearEnhancement != GearEnhancements.None)
                displayName += enhanceCode;

            return displayName;
        }

        if (WeapVariance != WeaponVariance.None && ItemQuality == Quality.Common)
        {
            var displayName = Template.Name + " of " + WeapVariance;

            if (GearEnhancement != GearEnhancements.None)
                displayName += enhanceCode;

            return displayName;
        }

        var standard = colorCode + Template.Name;

        if (GearEnhancement != GearEnhancements.None)
            standard += enhanceCode;

        return standard;
    }

    public string NoColorGetDisplayName()
    {
        var colorCode = ItemQuality switch
        {
            Quality.Damaged => "Damaged ",
            Quality.Common => "",
            Quality.Uncommon => "Uncommon ",
            Quality.Rare => "Rare ",
            Quality.Epic => "Epic ",
            Quality.Legendary => "Legendary ",
            Quality.Forsaken => "Forsaken ",
            Quality.Mythic => "Mythic ",
            Quality.Primordial => "Primordial ",
            Quality.Transcendent => "Transcendent ",
            _ => ""
        };

        var enhanceCode = GearEnhancement switch
        {
            GearEnhancements.None => "",
            GearEnhancements.One => " +1",
            GearEnhancements.Two => " +2",
            GearEnhancements.Three => " +3",
            GearEnhancements.Four => " +4",
            GearEnhancements.Five => " +5",
            GearEnhancements.Six => " +6",
            _ => ""
        };

        if (Tarnished)
            colorCode = "Tarnished ";

        if (!Enchantable) return Template.Name;
        if (ItemVariance != Variance.None && ItemQuality != Quality.Common)
        {
            var displayName = colorCode + Template.Name + " of " + ItemVariance;
            return displayName;
        }

        if (ItemVariance != Variance.None && ItemQuality == Quality.Common)
        {
            var displayName = Template.Name + " of " + ItemVariance;
            return displayName;
        }

        if (WeapVariance != WeaponVariance.None && ItemQuality != Quality.Common)
        {
            var displayName = colorCode + Template.Name + " of " + WeapVariance;

            if (GearEnhancement != GearEnhancements.None)
                displayName += enhanceCode;

            return displayName;
        }

        if (WeapVariance != WeaponVariance.None && ItemQuality == Quality.Common)
        {
            var displayName = Template.Name + " of " + WeapVariance;

            if (GearEnhancement != GearEnhancements.None)
                displayName += enhanceCode;

            return displayName;
        }

        var standard = colorCode + Template.Name;

        if (GearEnhancement != GearEnhancements.None)
            standard += enhanceCode;

        return standard;
    }

    public Item()
    {
        TileType = TileContent.Item;
    }

    /// <summary>
    /// Used to amend an ItemId if it exists during item creation
    /// </summary>
    /// <returns>ItemId</returns>
    private static long CheckAndAmendItemIdIfItExists(IItem item)
    {
        var updateIfExists = ServerSetup.Instance.GlobalGroundItemCache.TryGetValue(item.ItemId, out _);
        return updateIfExists ? EphemeralRandomIdGenerator<long>.Shared.NextId : item.ItemId;
    }

    public Item Create(Sprite owner, string item, Quality quality, Variance variance, WeaponVariance wVariance, bool curse = false) => !ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(item, out var value) ? null : Create(owner, value, quality, variance, wVariance, curse);
    public Item Create(Sprite owner, string item, bool curse = false) => !ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(item, out var value) ? null : Create(owner, value, curse);

    public Item Create(Sprite owner, ItemTemplate itemTemplate, Quality quality, Variance variance, WeaponVariance wVariance, bool curse = false)
    {
        if (owner == null) return null;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(itemTemplate.Name, out var value)) return null;

        var template = value ?? itemTemplate;
        var readyTime = DateTime.UtcNow;
        var obj = new Item
        {
            AbandonedDate = readyTime,
            Template = template,
            Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
            Image = template.Image,
            DisplayImage = template.DisplayImage,
            CurrentMapId = owner.CurrentMapId,
            Cursed = curse,
            MaxDurability = template.MaxDurability,
            Durability = template.MaxDurability,
            OffenseElement = template.OffenseElement,
            SecondaryOffensiveElement = template.SecondaryOffensiveElement,
            DefenseElement = template.DefenseElement,
            SecondaryDefensiveElement = template.SecondaryDefensiveElement,
            Enchantable = template.Enchantable,
            GearEnhancement = GearEnhancements.None,
            ItemMaterial = ItemMaterials.None,
            Warnings = new bool[3],
        };

        if (obj.Color == 0)
            obj.Color = 13;

        if (obj.Color != 0)
            obj.Color = (byte)template.Color;

        if (obj.Template.Flags.FlagIsSet(ItemFlags.Repairable))
        {
            if (obj.Template.MaxDurability == uint.MinValue)
            {
                obj.Template.MaxDurability = ServerSetup.Instance.Config.DefaultItemDurability;
                obj.Durability = ServerSetup.Instance.Config.DefaultItemDurability;
            }

            if (obj.Template.Value == uint.MinValue)
                obj.Template.Value = ServerSetup.Instance.Config.DefaultItemValue;
        }

        if (obj.Template.Flags.FlagIsSet(ItemFlags.QuestRelated))
        {
            obj.Template.MaxDurability = 0;
            obj.Durability = 0;
        }

        if (obj.Template.CanStack)
            obj.Enchantable = false;

        if (obj.Enchantable)
        {
            switch (obj.Template.EquipmentSlot)
            {
                case ItemSlots.Earring:
                case ItemSlots.Necklace:
                case ItemSlots.Waist:
                case ItemSlots.LArm:
                case ItemSlots.RArm:
                case ItemSlots.Leg:
                case ItemSlots.Foot:
                case ItemSlots.LHand:
                case ItemSlots.RHand:
                    obj.ItemVariance = variance;
                    obj.WeapVariance = WeaponVariance.None;
                    break;
                case ItemSlots.Weapon:
                case ItemSlots.Shield:
                    obj.ItemVariance = Variance.None;
                    obj.WeapVariance = wVariance;
                    break;
            }

            obj.ItemQuality = quality;

            var checkedQuality = QualityRestriction(obj);

            obj.ItemQuality = checkedQuality;
            obj.OriginalQuality = checkedQuality;
        }
        else
        {
            obj.ItemQuality = Quality.Common;
            obj.OriginalQuality = Quality.Common;
            obj.ItemVariance = Variance.None;
            obj.WeapVariance = WeaponVariance.None;
        }

        obj.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        obj.ItemId = EphemeralRandomIdGenerator<long>.Shared.NextId;
        obj.ItemId = CheckAndAmendItemIdIfItExists(obj);

        obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
        if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
            obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

        return obj;
    }

    public Item Create(Sprite owner, ItemTemplate itemTemplate, bool curse = false)
    {
        if (owner == null) return null;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(itemTemplate.Name, out var value)) return null;

        var template = value ?? itemTemplate;
        var readyTime = DateTime.UtcNow;
        var obj = new Item
        {
            AbandonedDate = readyTime,
            Template = template,
            Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
            Image = template.Image,
            DisplayImage = template.DisplayImage,
            CurrentMapId = owner.CurrentMapId,
            Cursed = curse,
            MaxDurability = template.MaxDurability,
            Durability = template.MaxDurability,
            OffenseElement = template.OffenseElement,
            SecondaryOffensiveElement = template.SecondaryOffensiveElement,
            DefenseElement = template.DefenseElement,
            SecondaryDefensiveElement = template.SecondaryDefensiveElement,
            Warnings = new bool[3],
            Enchantable = false,
            ItemQuality = Quality.Common,
            OriginalQuality = Quality.Common,
            ItemVariance = Variance.None,
            WeapVariance = WeaponVariance.None,
            GearEnhancement = GearEnhancements.None,
            ItemMaterial = ItemMaterials.None
        };

        if (obj.Color == 0)
            obj.Color = 13;

        if (obj.Color != 0)
            obj.Color = (byte)template.Color;

        if (obj.Template.Flags.FlagIsSet(ItemFlags.Repairable))
        {
            if (obj.Template.MaxDurability == uint.MinValue)
            {
                obj.Template.MaxDurability = ServerSetup.Instance.Config.DefaultItemDurability;
                obj.Durability = ServerSetup.Instance.Config.DefaultItemDurability;
            }

            if (obj.Template.Value == uint.MinValue)
                obj.Template.Value = ServerSetup.Instance.Config.DefaultItemValue;
        }

        if (obj.Template.Flags.FlagIsSet(ItemFlags.QuestRelated))
        {
            obj.Template.MaxDurability = 0;
            obj.Durability = 0;
        }

        obj.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        obj.ItemId = EphemeralRandomIdGenerator<long>.Shared.NextId;
        obj.ItemId = CheckAndAmendItemIdIfItExists(obj);

        obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
        if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
            obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

        return obj;
    }

    /// <summary>
    /// Owner is not populated - Used for Map populated items
    /// </summary>
    public Item Create(Area map, ItemTemplate itemTemplate)
    {
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(itemTemplate.Name, out var value)) return null;

        var template = value ?? itemTemplate;
        var readyTime = DateTime.UtcNow;
        var obj = new Item
        {
            AbandonedDate = readyTime,
            Template = template,
            Pos = Vector2.Zero,
            Image = template.Image,
            DisplayImage = template.DisplayImage,
            CurrentMapId = map.ID,
            Cursed = false,
            MaxDurability = template.MaxDurability,
            Durability = template.MaxDurability,
            OffenseElement = template.OffenseElement,
            SecondaryOffensiveElement = template.SecondaryOffensiveElement,
            DefenseElement = template.DefenseElement,
            SecondaryDefensiveElement = template.SecondaryDefensiveElement,
            Warnings = new bool[3],
            Enchantable = false,
            ItemQuality = Quality.Common,
            OriginalQuality = Quality.Common,
            ItemVariance = Variance.None,
            WeapVariance = WeaponVariance.None,
            GearEnhancement = GearEnhancements.None,
            ItemMaterial = ItemMaterials.None
        };

        if (obj.Template.Flags.FlagIsSet(ItemFlags.QuestRelated))
        {
            obj.Template.MaxDurability = 0;
            obj.Durability = 0;
        }

        obj.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        obj.ItemId = EphemeralRandomIdGenerator<long>.Shared.NextId;
        obj.ItemId = CheckAndAmendItemIdIfItExists(obj);

        obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
        if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
            obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

        ServerSetup.Instance.GlobalGroundItemCache.TryAdd(obj.ItemId, obj);
        return obj;
    }

    public Quality QualityRestriction(Item item)
    {
        switch (item.Template.LevelRequired)
        {
            case >= 1 and <= 30:
                if (item.ItemQuality is Quality.Mythic or Quality.Forsaken or Quality.Legendary or Quality.Epic)
                {
                    return item.ItemQuality = Quality.Rare;
                }
                break;
            case <= 98:
                if (item.ItemQuality is Quality.Mythic or Quality.Forsaken)
                {
                    return item.ItemQuality = Quality.Legendary;
                }
                break;
            case <= 125:
                if (item.ItemQuality is Quality.Mythic)
                {
                    return item.ItemQuality = Quality.Forsaken;
                }
                break;
        }

        return item.ItemQuality;
    }

    public Trap TrapCreate(Sprite owner, ItemTemplate itemTemplate, int duration, int radius = 1, Action<Sprite, Sprite> cb = null)
    {
        if (owner == null) return null;

        var readyTime = DateTime.UtcNow;
        var obj = new Item
        {
            AbandonedDate = readyTime,
            Template = itemTemplate,
            Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
            Image = itemTemplate.Image,
            DisplayImage = itemTemplate.DisplayImage,
            CurrentMapId = owner.CurrentMapId,
            OffenseElement = itemTemplate.OffenseElement,
            SecondaryOffensiveElement = itemTemplate.SecondaryOffensiveElement,
            DefenseElement = itemTemplate.DefenseElement,
            SecondaryDefensiveElement = itemTemplate.SecondaryDefensiveElement,
            Enchantable = false,
            ItemQuality = Quality.Common,
            OriginalQuality = Quality.Common,
            ItemVariance = Variance.None,
            WeapVariance = WeaponVariance.None,
            GearEnhancement = GearEnhancements.None,
            ItemMaterial = ItemMaterials.None,
            Serial = owner.Serial,
            ItemId = EphemeralRandomIdGenerator<long>.Shared.NextId
        };

        obj.ItemId = CheckAndAmendItemIdIfItExists(obj);

        return new Trap
        {
            Radius = radius,
            Duration = duration,
            CurrentMapId = obj.CurrentMapId,
            Location = obj.Position,
            Owner = owner,
            Tripped = cb,
            Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId,
            TrapItem = obj
        };
    }

    public bool CanCarry(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return true;
        if (aisling.CurrentWeight + Template.CarryWeight <= aisling.MaximumWeight) return true;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You are now overburden");
        return false;
    }

    public bool GiveTo(Sprite sprite, bool checkWeight = true)
    {
        if (sprite is not Aisling aisling) return false;
        ItemPane = ItemPanes.Inventory;

        if (aisling.Inventory.IsFull)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
            return false;
        }

        // Stack
        if (Template.Flags.FlagIsSet(ItemFlags.Stackable))
        {
            var numStacks = (byte)Stacks;

            if (numStacks <= 0)
                numStacks = 1;

            var item = aisling.Inventory.Items.Values.FirstOrDefault(i => i != null && i.Template.Name == Template.Name && i.Stacks + numStacks <= i.Template.MaxStack);

            if (item != null)
            {
                // Add count to an existing stack
                aisling.Inventory.AddRange(aisling.Client, item, numStacks);

                // Delete existing item since we're adding it to another stack
                aisling.Inventory.RemoveFromInventory(aisling.Client, this);

                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"Received {DisplayName}, You now have {(item.Stacks == 0 ? item.Stacks + 1 : item.Stacks)}");
                aisling.Client.SendAttributes(StatUpdateType.Primary);
                aisling.Client.SendAttributes(StatUpdateType.ExpGold);
                return true;
            }

            if (Stacks <= 0)
                Stacks = 1;

            InventorySlot = aisling.Inventory.FindEmpty();

            if (InventorySlot >= 60)
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantCarryMoreMsg}");
                return false;
            }

            aisling.Inventory.Items.TryUpdate(InventorySlot, this, null);
            aisling.Inventory.UpdateSlot(aisling.Client, this);
            aisling.Client.SendAttributes(StatUpdateType.Primary);
            aisling.Client.SendAttributes(StatUpdateType.ExpGold);
            return true;
        }

        // Non-Stack
        InventorySlot = aisling.Inventory.FindEmpty();

        if (InventorySlot == byte.MaxValue)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantCarryMoreMsg}");
            return false;
        }

        aisling.Inventory.Items.TryUpdate(InventorySlot, this, null);
        aisling.Inventory.UpdateSlot(aisling.Client, this);
        aisling.Client.SendAttributes(StatUpdateType.Primary);
        aisling.Client.SendAttributes(StatUpdateType.ExpGold);
        return true;
    }

    public void Release(Sprite owner, Position position)
    {
        Pos = new Vector2(position.X, position.Y);
        ItemPane = ItemPanes.Ground;
        CurrentMapId = owner.CurrentMapId;
        AbandonedDate = DateTime.UtcNow;
        Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        ItemPane = ItemPanes.Ground;
        ServerSetup.Instance.GlobalGroundItemCache.TryAdd(ItemId, this);
        AddObject(this);

        if (owner is Aisling player)
            ShowTo(player);
    }

    /// <summary>
    /// Delete from database only happens on death, or item dropped
    /// Checks if item still exists on SQL List, if so remove
    /// </summary>
    public async void DeleteFromAislingDb()
    {
        var itemId = ItemId;
        await StorageManager.AislingBucket.SaveLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersItems WHERE ItemId = @ItemId";
            sConn.Execute(cmd, new { itemId });
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            StorageManager.AislingBucket.SaveLock.Release();
        }
    }

    /// <summary>
    /// Removes all item bonuses, then reapplies them on item change
    /// </summary>
    public void ReapplyItemModifiers(WorldClient client)
    {
        if (client?.Aisling == null) return;
        ItemPane = ItemPanes.Equip;

        lock (client.SyncClient)
        {
            try
            {
                // Removes modifiers
                RemoveModifiers(client);

                // Recalculates Spell Lines
                SpellLines(client);

                foreach (var equipment in client.Aisling.EquipmentManager.Equipment)
                {
                    if (equipment.Value == null) continue;

                    // Reapplies Stat modifiers
                    StatModifiersCalc(client, equipment.Value.Item);

                    if (!equipment.Value.Item.Template.Enchantable) continue;

                    ItemVarianceCalc(client, equipment.Value.Item);
                    WeaponVarianceCalc(client, equipment.Value.Item);
                    QualityVarianceCalc(client, equipment.Value.Item);
                }

                // Reapplies Removed Buffs/Debuffs
                BuffDebuffCalc(client);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
        }

        client.SendAttributes(StatUpdateType.Full);
        var ac = client.Aisling.SealedAc.ToString();
        var regen = client.Aisling.Regen.ToString();
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=sAC{{=c: {{=a{ac}{{=c, {{=sRegen{{=c: {{=a{regen}");
    }

    public void RemoveModifiers(WorldClient client)
    {
        if (client?.Aisling == null) return;
        client.Aisling.BonusAc = 0;
        client.Aisling.BonusMr = 0;
        client.Aisling.BonusRegen = 0;

        client.Aisling.BonusHp = 0;
        client.Aisling.BonusMp = 0;

        client.Aisling.BonusStr = 0;
        client.Aisling.BonusInt = 0;
        client.Aisling.BonusWis = 0;
        client.Aisling.BonusCon = 0;
        client.Aisling.BonusDex = 0;

        client.Aisling.BonusHit = 0;
        client.Aisling.BonusDmg = 0;

        client.Aisling.Spikes = 0;
        client.Aisling.Bleeding = 0;
        client.Aisling.Rending = 0;
        client.Aisling.Aegis = 0;
        client.Aisling.Reaping = 0;
        client.Aisling.Vampirism = 0;
        client.Aisling.Ghosting = 0;
        client.Aisling.Haste = 0;
        client.Aisling.Gust = 0;
        client.Aisling.Quake = 0;
        client.Aisling.Rain = 0;
        client.Aisling.Flame = 0;
        client.Aisling.Dusk = 0;
        client.Aisling.Dawn = 0;
    }

    public void StatModifiersCalc(WorldClient client, Item equipment)
    {
        client.Aisling.BonusAc += equipment.Template.AcModifer;
        client.Aisling.BonusMr += equipment.Template.MrModifer;
        client.Aisling.BonusHp += equipment.Template.HealthModifer;
        client.Aisling.BonusMp += equipment.Template.ManaModifer;
        client.Aisling.BonusRegen += equipment.Template.RegenModifer;
        client.Aisling.BonusStr += equipment.Template.StrModifer;
        client.Aisling.BonusInt += equipment.Template.IntModifer;
        client.Aisling.BonusWis += equipment.Template.WisModifer;
        client.Aisling.BonusCon += equipment.Template.ConModifer;
        client.Aisling.BonusDex += equipment.Template.DexModifer;
        client.Aisling.BonusHit += equipment.Template.HitModifer;
        client.Aisling.BonusDmg += equipment.Template.DmgModifer;
    }

    public void SpellLines(WorldClient client)
    {
        for (var i = 0; i < client.Aisling.SpellBook.Spells.Count; i++)
        {
            var spell = client.Aisling.SpellBook.FindInSlot(i);

            if (spell?.Template == null) continue;
            spell.Lines = spell.Template.BaseLines;

            // Calculate Spell lines from off-hand first
            if (client.Aisling.EquipmentManager.Equipment[3]?.Item != null)
            {
                var offHand = client.Aisling.EquipmentManager.Equipment[3].Item;
                var op = offHand.Template.IsPositiveSpellLines;

                if (op != 0)
                {
                    switch (op)
                    {
                        case 1:
                            spell.Lines += offHand.Template.SpellLinesModifier;
                            break;
                        case 2:
                            spell.Lines -= offHand.Template.SpellLinesModifier;
                            break;
                    }
                }
            }

            // Calculate Spell lines from weapon second
            if (client.Aisling.EquipmentManager.Equipment[1]?.Item != null)
            {
                var weapon = client.Aisling.EquipmentManager.Equipment[1].Item;
                var op = weapon.Template.IsPositiveSpellLines;

                if (op != 0)
                {
                    switch (op)
                    {
                        case 1:
                            spell.Lines += weapon.Template.SpellLinesModifier;
                            break;
                        case 2:
                            spell.Lines -= weapon.Template.SpellLinesModifier;
                            break;
                    }
                }
            }

            if (spell.Lines > spell.Template.MaxLines)
                spell.Lines = spell.Template.MaxLines;

            if (spell.Lines < 0) spell.Lines = 0;

            UpdateSpell(client, spell);
        }
    }

    public void ItemVarianceCalc(WorldClient client, Item equipment)
    {
        Dictionary<Variance, Action<WorldClient>> varianceActions = new()
        {
            {Variance.Embunement, c => c.Aisling.BonusHit += 5},
            {Variance.Blessing, c => c.Aisling.BonusDmg += 2},
            {Variance.Mana, c => c.Aisling.BonusMp += 250},
            {Variance.Gramail, c => c.Aisling.BonusMr += 10},
            {Variance.Deoch, c => { c.Aisling.BonusRegen += 10; c.Aisling.BonusAc -= 2; }},
            {Variance.Ceannlaidir, c => c.Aisling.BonusStr += 1},
            {Variance.Cail, c => c.Aisling.BonusCon += 1},
            {Variance.Fiosachd, c => c.Aisling.BonusDex += 1},
            {Variance.Glioca, c => c.Aisling.BonusWis += 1},
            {Variance.Luathas, c => c.Aisling.BonusInt += 1},
            {Variance.Sgrios, c => { c.Aisling.BonusStr += 2; c.Aisling.BonusAc += 1; c.Aisling.BonusRegen -= 10; }},
            {Variance.Reinforcement, c => { c.Aisling.BonusAc += 1; c.Aisling.BonusHp += 250; }},
            {Variance.Spikes, c => c.Aisling.Spikes += 1}
        };

        if (varianceActions.TryGetValue(equipment.ItemVariance, out var action))
        {
            action(client);
        }
    }

    public void WeaponVarianceCalc(WorldClient client, Item equipment)
    {
        Dictionary<WeaponVariance, Action<WorldClient>> varianceActions = new()
        {
            {WeaponVariance.Bleeding, c => c.Aisling.Bleeding += 1},
            {WeaponVariance.Rending, c => c.Aisling.Rending += 1},
            {WeaponVariance.Aegis, c => c.Aisling.Aegis += 1},
            {WeaponVariance.Reaping, c => c.Aisling.Reaping += 1},
            {WeaponVariance.Vampirism, c => c.Aisling.Vampirism += 1},
            {WeaponVariance.Ghosting, c => c.Aisling.Ghosting += 1},
            {WeaponVariance.Haste, c => c.Aisling.Haste += 1},
            {WeaponVariance.Gust, c => c.Aisling.Gust += 1},
            {WeaponVariance.Quake, c => c.Aisling.Quake += 1},
            {WeaponVariance.Rain, c => c.Aisling.Rain += 1},
            {WeaponVariance.Flame, c => c.Aisling.Flame += 1},
            {WeaponVariance.Dusk, c => c.Aisling.Dusk += 1},
            {WeaponVariance.Dawn, c => c.Aisling.Dawn += 1},
        };

        if (varianceActions.TryGetValue(equipment.WeapVariance, out var action))
        {
            action(client);
        }
    }

    public void QualityVarianceCalc(WorldClient client, Item equipment)
    {
        if (Tarnished && equipment.ItemQuality != Quality.Damaged) return;

        Dictionary<Quality, QualityBonus> qualityBonuses = new()
        {
            {Quality.Damaged, new QualityBonus(0, 0, 0, 0, 0, 0, 0, -2, 0, 0, 0, 0)},
            {Quality.Common, new QualityBonus(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)},
            {Quality.Uncommon, new QualityBonus(1, 1, 1, 1, 1, 1, 0, 0, 0, 100, 0, 0)},
            {Quality.Rare, new QualityBonus(1, 1, 1, 1, 1, 2, 5, 1, 5, 500, 100, 0)},
            {Quality.Epic, new QualityBonus(2, 2, 2, 2, 2, 2, 10, 1, 10, 750, 250, 5)},
            {Quality.Legendary, new QualityBonus(3, 3, 3, 3, 3, 15, 20, 2, 20, 1000, 500, 10)},
            {Quality.Forsaken, new QualityBonus(4, 4, 4, 4, 4, 20, 25, 3, 25, 1500, 1000, 20)},
            {Quality.Mythic, new QualityBonus(5, 5, 5, 5, 5, 25, 30, 5, 30, 2500, 2000, 30)}
        };

        if (!qualityBonuses.TryGetValue(equipment.ItemQuality, out var bonus)) return;

        client.Aisling.BonusStr += bonus.Str;
        client.Aisling.BonusInt += bonus.Int;
        client.Aisling.BonusWis += bonus.Wis;
        client.Aisling.BonusCon += bonus.Con;
        client.Aisling.BonusDex += bonus.Dex;
        client.Aisling.BonusDmg += bonus.Dmg;
        client.Aisling.BonusHit += bonus.Hit;
        client.Aisling.BonusAc += bonus.Ac;
        client.Aisling.BonusMr += bonus.Mr;
        client.Aisling.BonusHp += bonus.Hp;
        client.Aisling.BonusMp += bonus.Mp;
        client.Aisling.BonusRegen += bonus.Regen;
    }

    public void BuffDebuffCalc(WorldClient client)
    {
        Parallel.ForEach(client.Aisling.Buffs.Values, (buff) =>
        {
            buff.OnItemChange(client.Aisling, buff);
        });

        Parallel.ForEach(client.Aisling.Debuffs.Values, (debuff) =>
        {
            debuff.OnItemChange(client.Aisling, debuff);
        });
    }

    public void UpdateSpell(WorldClient client, Spell spell)
    {
        client.SendRemoveSpellFromPane(spell.Slot);
        client.SendAddSpellToPane(spell);
    }
}

public class QualityBonus(int _str, int _int, int _wis, int _con, int _dex, int dmg, int hit, int ac, int mr, int hp, int mp, int regen)
{
    public int Str { get; } = _str;
    public int Int { get; } = _int;
    public int Wis { get; } = _wis;
    public int Con { get; } = _con;
    public int Dex { get; } = _dex;
    public int Dmg { get; } = dmg;
    public int Hit { get; } = hit;
    public int Ac { get; } = ac;
    public int Mr { get; } = mr;
    public int Hp { get; } = hp;
    public int Mp { get; } = mp;
    public int Regen { get; } = regen;
}