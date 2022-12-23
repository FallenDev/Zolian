using System.Collections.Concurrent;
using System.Data;
using System.Numerics;

using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Sprites
{
    public sealed class Item : Sprite, IItem
    {
        public enum Quality
        {
            Damaged,
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary,
            Forsaken,
            Mythic
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
            Haste,
            Gust,
            Quake,
            Rain,
            Flame,
            Dusk,
            Dawn
        }

        public enum GearEnhancement
        {
            None,
            One,
            Two,
            Three,
            Four,
            Five,
            Six
        }

        public enum ItemMaterial
        {
            Copper,
            Iron,
            Steel,
            Forged,
            Elven,
            Dwarven,
            Mythril,
            Hybrasyl,
            Ebony,
            Chaos,
            MoonStone,
            SunStone,
            Runic
        }

        private SemaphoreSlim CreateLock { get; } = new(1, 1);
        public Sprite[] AuthenticatedAislings { get; set; }
        public byte Color { get; set; }
        public bool Cursed { get; set; }
        public ushort DisplayImage { get; init; }
        public string Name { get; set; }
        public string DisplayName => GetDisplayName();
        public string NoColorDisplayName => NoColorGetDisplayName();
        public uint Durability { get; set; }
        public uint MaxDurability { get; set; }
        public bool Equipped { get; set; }
        public bool Identified { get; init; }
        public ushort Image { get; init; }
        public Variance ItemVariance { get; set; }
        public WeaponVariance WeapVariance { get; set; }
        public Quality ItemQuality { get; set; }
        public Quality OriginalQuality { get; set; }
        public int ItemId { get; set; }
        public int Owner { get; set; }
        public ConcurrentDictionary<string, ItemScript> Scripts { get; set; }
        public byte InventorySlot { get; set; }
        public byte Slot { get; set; }
        public ushort Stacks { get; set; }
        public int Dropping { get; set; }
        public ItemTemplate Template { get; set; }
        public bool Enchantable { get; set; }
        public bool[] Warnings { get; init; }
        public ConcurrentDictionary<string, WeaponScript> WeaponScripts { get; set; }

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
                Quality.Mythic => "{=fMythic",
                _ => ""
            };

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
                return displayName;
            }

            if (WeapVariance != WeaponVariance.None && ItemQuality == Quality.Common)
            {
                var displayName = Template.Name + " of " + WeapVariance;
                return displayName;
            }

            var standard = colorCode + Template.Name;

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
                _ => ""
            };

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
                return displayName;
            }

            if (WeapVariance != WeaponVariance.None && ItemQuality == Quality.Common)
            {
                var displayName = Template.Name + " of " + WeapVariance;
                return displayName;
            }

            var standard = colorCode + Template.Name;

            return standard;
        }

        public Item()
        {
            EntityType = TileContent.Item;
        }

        public Item Create(Sprite owner, string item, Quality quality, Variance variance,
            WeaponVariance wVariance, bool curse = false)
        {
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item))
                return null;
            var template = ServerSetup.Instance.GlobalItemTemplateCache[item];
            return Create(owner, template, quality, variance, wVariance, curse);
        }

        public Item Create(Sprite owner, ItemTemplate itemTemplate, Quality quality, Variance variance,
            WeaponVariance wVariance, bool curse = false)
        {
            if (owner == null) return null;
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(itemTemplate.Name)) return null;

            var template = ServerSetup.Instance.GlobalItemTemplateCache[itemTemplate.Name] ?? itemTemplate;
            var readyTime = DateTime.Now;
            var obj = new Item
            {
                AbandonedDate = readyTime,
                Template = template,
                Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
                Image = template.Image,
                DisplayImage = template.DisplayImage,
                CurrentMapId = owner.CurrentMapId,
                Cursed = curse,
                Owner = owner.Serial,
                MaxDurability = template.MaxDurability,
                Durability = template.MaxDurability,
                OffenseElement = template.OffenseElement,
                SecondaryOffensiveElement = template.SecondaryOffensiveElement,
                DefenseElement = template.DefenseElement,
                SecondaryDefensiveElement = template.SecondaryDefensiveElement,
                Enchantable = template.Enchantable,
                Warnings = new bool[3],
                AuthenticatedAislings = null
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

            obj.Serial = Generator.GenerateNumber();
            obj.ItemId = Generator.GenerateNumber();
            obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
            if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
                obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

            return obj;
        }

        public Item Create(Sprite owner, string item, bool curse = false)
        {
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item))
                return null;
            var template = ServerSetup.Instance.GlobalItemTemplateCache[item];
            return Create(owner, template, curse);
        }

        public Item Create(Sprite owner, ItemTemplate itemTemplate, bool curse = false)
        {
            if (owner == null) return null;
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(itemTemplate.Name)) return null;

            var template = ServerSetup.Instance.GlobalItemTemplateCache[itemTemplate.Name] ?? itemTemplate;
            var readyTime = DateTime.Now;
            var obj = new Item
            {
                AbandonedDate = readyTime,
                Template = template,
                Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
                Image = template.Image,
                DisplayImage = template.DisplayImage,
                CurrentMapId = owner.CurrentMapId,
                Cursed = curse,
                Owner = owner.Serial,
                MaxDurability = template.MaxDurability,
                Durability = template.MaxDurability,
                OffenseElement = template.OffenseElement,
                SecondaryOffensiveElement = template.SecondaryOffensiveElement,
                DefenseElement = template.DefenseElement,
                SecondaryDefensiveElement = template.SecondaryDefensiveElement,
                Warnings = new bool[3],
                AuthenticatedAislings = null,
                Enchantable = false,
                ItemQuality = Quality.Common,
                OriginalQuality = Quality.Common,
                ItemVariance = Variance.None,
                WeapVariance = WeaponVariance.None
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

            obj.Serial = Generator.GenerateNumber();
            obj.ItemId = Generator.GenerateNumber();
            obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
            if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
                obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

            return obj;
        }

        public Item Create(Area map, ItemTemplate itemTemplate)
        {
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(itemTemplate.Name)) return null;

            var template = ServerSetup.Instance.GlobalItemTemplateCache[itemTemplate.Name] ?? itemTemplate;
            var readyTime = DateTime.Now;
            var obj = new Item
            {
                AbandonedDate = readyTime,
                Template = template,
                Pos = Vector2.Zero,
                Image = template.Image,
                DisplayImage = template.DisplayImage,
                CurrentMapId = map.ID,
                Cursed = false,
                Owner = 0,
                MaxDurability = template.MaxDurability,
                Durability = template.MaxDurability,
                OffenseElement = template.OffenseElement,
                SecondaryOffensiveElement = template.SecondaryOffensiveElement,
                DefenseElement = template.DefenseElement,
                SecondaryDefensiveElement = template.SecondaryDefensiveElement,
                Warnings = new bool[3],
                AuthenticatedAislings = null,
                Enchantable = false,
                ItemQuality = Quality.Common,
                OriginalQuality = Quality.Common,
                ItemVariance = Variance.None,
                WeapVariance = WeaponVariance.None
            };

            if (obj.Template.Flags.FlagIsSet(ItemFlags.QuestRelated))
            {
                obj.Template.MaxDurability = 0;
                obj.Durability = 0;
            }

            obj.Serial = Generator.GenerateNumber();
            obj.ItemId = Generator.GenerateNumber();
            obj.Scripts = ScriptManager.Load<ItemScript>(template.ScriptName, obj);
            if (!string.IsNullOrEmpty(obj.Template.WeaponScript))
                obj.WeaponScripts = ScriptManager.Load<WeaponScript>(obj.Template.WeaponScript, obj);

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
                case >= 31 and <= 60:
                    if (item.ItemQuality is Quality.Mythic or Quality.Forsaken or Quality.Legendary)
                    {
                        return item.ItemQuality = Quality.Epic;
                    }

                    break;
                case >= 61 and <= 98:
                    if (item.ItemQuality is Quality.Mythic or Quality.Forsaken)
                    {
                        return item.ItemQuality = Quality.Legendary;
                    }

                    break;
                case >= 99 and <= 249:
                    if (item.ItemQuality is Quality.Mythic)
                    {
                        return item.ItemQuality = Quality.Forsaken;
                    }

                    break;
            }

            return item.ItemQuality;
        }

        public Item TrapCreate(Sprite owner, ItemTemplate itemTemplate)
        {
            if (owner == null) return null;

            var readyTime = DateTime.Now;
            var obj = new Item
            {
                AbandonedDate = readyTime,
                Template = itemTemplate,
                Pos = new Vector2((int)owner.Pos.X, (int)owner.Pos.Y),
                Image = itemTemplate.Image,
                DisplayImage = itemTemplate.DisplayImage,
                CurrentMapId = owner.CurrentMapId,
                Owner = owner.Serial,
                OffenseElement = itemTemplate.OffenseElement,
                SecondaryOffensiveElement = itemTemplate.SecondaryOffensiveElement,
                DefenseElement = itemTemplate.DefenseElement,
                SecondaryDefensiveElement = itemTemplate.SecondaryDefensiveElement,
                Enchantable = false,
                ItemQuality = Quality.Common,
                OriginalQuality = Quality.Common,
                ItemVariance = Variance.None,
                WeapVariance = WeaponVariance.None
            };

            obj.Serial = Generator.GenerateNumber();
            obj.ItemId = Generator.GenerateNumber();

            return obj;
        }

        public bool CanCarry(Sprite sprite)
        {
            if (sprite is not Aisling aisling) return true;
            if (aisling.CurrentWeight + Template.CarryWeight <= aisling.MaximumWeight) return true;
            aisling.Client.SendMessage(Scope.Self, 0x02, $"{ServerSetup.Instance.Config.ToWeakToLift}");
            return false;
        }

        public bool GiveTo(Sprite sprite, bool checkWeight = true)
        {
            if (sprite is not Aisling aisling) return false;
            Owner = aisling.Serial;

            #region stackable items

            if (Template.Flags.FlagIsSet(ItemFlags.Stackable))
            {
                var numStacks = (byte)Stacks;

                if (numStacks <= 0)
                    numStacks = 1;

                var item = aisling.Inventory.Items.Values.FirstOrDefault(i => i != null && i.Template.Name == Template.Name && i.Stacks + numStacks < i.Template.MaxStack);

                if (item != null)
                {
                    aisling.Inventory.AddRange(aisling.Client, item, numStacks);
                    aisling.Client.SendMessage(Scope.Self, 0x02, $"Received {DisplayName}, You now have {(item.Stacks == 0 ? item.Stacks + 1 : item.Stacks)}");
                    aisling.Client.SendStats(StatusFlags.WeightMoney);

                    return true;
                }

                if (Stacks <= 0)
                    Stacks = 1;

                if (checkWeight)
                    if (!CanCarry(aisling))
                        return false;

                InventorySlot = aisling.Inventory.FindEmpty();

                if (InventorySlot >= 60)
                {
                    aisling.Client.SendMessage(Scope.Self, 0x02, $"{ServerSetup.Instance.Config.CantCarryMoreMsg}");
                    return false;
                }

                aisling.Client.Send(new ServerFormat10(InventorySlot));
                aisling.Inventory.Set(this);
                aisling.Inventory.UpdateSlot(aisling.Client, this);
                AddToAislingDb(aisling);
                aisling.Inventory.UpdatePlayersWeight(aisling.Client);
                aisling.Client?.SendStats(StatusFlags.WeightMoney);

                return true;
            }

            #endregion

            #region not stackable items

            {
                InventorySlot = aisling.Inventory.FindEmpty();

                if (InventorySlot == byte.MaxValue)
                {
                    aisling.Client.SendMessage(Scope.Self, 0x02, $"{ServerSetup.Instance.Config.CantCarryMoreMsg}");
                    return false;
                }

                if (checkWeight)
                    if (!CanCarry(aisling))
                        return false;

                aisling.Inventory.Set(this);
                aisling.Inventory.UpdateSlot(aisling.Client, this);
                AddToAislingDb(aisling);
                aisling.Inventory.UpdatePlayersWeight(aisling.Client);
                aisling.Client?.SendStats(StatusFlags.WeightMoney);

                return true;
            }

            #endregion

        }

        public void Release(Sprite owner, Position position, bool delete = true)
        {
            Pos = new Vector2(position.X, position.Y);

            var readyTime = DateTime.Now;
            CurrentMapId = owner.CurrentMapId;
            AbandonedDate = readyTime;

            if (owner is Aisling)
            {
                AuthenticatedAislings = Array.Empty<Sprite>();
                Cursed = false;
                if (delete) DeleteFromAislingDb();
            }

            Serial = Generator.GenerateNumber();
            ItemId = Generator.GenerateNumber();

            AddObject(this);

            if (owner is Aisling player)
                ShowTo(player);
        }

        public void ReleaseFromEquipped(Sprite owner, Position position, bool delete = true)
        {
            Pos = new Vector2(position.X, position.Y);

            var readyTime = DateTime.Now;
            CurrentMapId = owner.CurrentMapId;
            AbandonedDate = readyTime;

            if (owner is Aisling)
            {
                AuthenticatedAislings = Array.Empty<Sprite>();
                Cursed = false;
                if (delete) DeleteFromAislingDbEquipped();
            }

            Serial = Generator.GenerateNumber();
            ItemId = Generator.GenerateNumber();

            AddObject(this);

            if (owner is Aisling player)
                ShowTo(player);
        }

        public async void AddToAislingDb(ISprite aisling)
        {
            await CreateLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("ItemToInventory", sConn);
                cmd.CommandType = CommandType.StoredProcedure;

                var color = ItemColors.ItemColorsToInt(Template.Color);
                var quality = ItemEnumConverters.QualityToString(ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(WeapVariance);

                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = ItemId;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = Template.Name;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
                cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
                cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = Cursed;
                cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = Durability;
                cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = Identified;
                cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
                cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
                cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
                cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
                cmd.Parameters.Add("@InventorySlot", SqlDbType.Int).Value = InventorySlot;
                cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = Stacks;
                cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = Enchantable;

                cmd.CommandTimeout = 5;
                cmd.ExecuteNonQuery();
                sConn.Close();
            }
            catch (SqlException e)
            {
                if (e.Message.Contains("PK__Players"))
                {
                    aisling.Client.SendMessage(0x03, "Item did not save correctly. Contact GM");
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

        public void DeleteFromAislingDb()
        {
            if (ItemId == 0) return;

            try
            {
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersInventory WHERE ItemId = @ItemId";
                sConn.Execute(cmd, new { ItemId });
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

        public void DeleteFromAislingDbEquipped()
        {
            if (ItemId == 0) return;

            try
            {
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersEquipped WHERE ItemId = @ItemId";
                sConn.Execute(cmd, new { ItemId });
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

        public void ApplyModifiers(GameClient client)
        {
            if (client?.Aisling == null) return;

            try
            {
                StatModifiersCalc(client, true);

                if (!Template.Enchantable)
                {
                    client.SendStats(StatusFlags.MultiStat);
                    var armor = client.Aisling.Ac.ToString();
                    var regenNoEnchant = client.Aisling.Regen.ToString();
                    client.SendMessage(0x03, $"{{=sAC{{=c: {{=a{armor}{{=c, {{=sRegen{{=c: {{=a{regenNoEnchant}");
                    return;
                }

                ItemVarianceCalc(client, true);
                WeaponVarianceCalc(client, true);
                QualityVarianceCalc(client, true);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            client.SendStats(StatusFlags.MultiStat);
            var ac = client.Aisling.Ac.ToString();
            var regen = client.Aisling.Regen.ToString();
            client.SendMessage(0x03, $"{{=sAC{{=c: {{=a{ac}{{=c, {{=sRegen{{=c: {{=a{regen}");
        }

        public void RemoveModifiers(GameClient client)
        {
            if (client?.Aisling == null) return;

            try
            {
                StatModifiersCalc(client, false);

                if (!Template.Enchantable)
                {
                    client.SendStats(StatusFlags.MultiStat);
                    return;
                }

                ItemVarianceCalc(client, false);
                WeaponVarianceCalc(client, false);
                QualityVarianceCalc(client, false);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            client.SendStats(StatusFlags.MultiStat);
        }

        public void StatModifiersCalc(GameClient client, bool isPositive)
        {
            if (Template.EquipmentSlot is 1 or 3)
                SpellLines(client, isPositive);

            switch (isPositive)
            {
                case true:
                    {
                        if (Template.AcModifer != 0)
                        {
                            client.Aisling.BonusAc += Template.AcModifer;
                            client.SendStats(StatusFlags.StructD);
                        }

                        if (Template.MrModifer != 0)
                        {
                            client.Aisling.BonusMr += (byte)Template.MrModifer;
                        }

                        if (Template.HealthModifer != 0)
                        {
                            client.Aisling.BonusHp += Template.HealthModifer;
                        }

                        if (Template.ManaModifer != 0)
                        {
                            client.Aisling.BonusMp += Template.ManaModifer;
                        }

                        if (Template.RegenModifer != 0)
                        {
                            client.Aisling.BonusRegen += Template.RegenModifer;
                        }

                        if (Template.StrModifer != 0)
                        {
                            client.Aisling.BonusStr += Template.StrModifer;
                        }

                        if (Template.IntModifer != 0)
                        {
                            client.Aisling.BonusInt += Template.IntModifer;
                        }

                        if (Template.WisModifer != 0)
                        {
                            client.Aisling.BonusWis += Template.WisModifer;
                        }

                        if (Template.ConModifer != 0)
                        {
                            client.Aisling.BonusCon += Template.ConModifer;
                        }

                        if (Template.DexModifer != 0)
                        {
                            client.Aisling.BonusDex += Template.DexModifer;
                        }

                        if (Template.HitModifer != 0)
                        {
                            client.Aisling.BonusHit += (byte)Template.HitModifer;
                        }

                        if (Template.DmgModifer != 0)
                        {
                            client.Aisling.BonusDmg += (byte)Template.DmgModifer;
                        }
                    }
                    break;
                case false:
                    {
                        if (Template.AcModifer != 0)
                        {
                            client.Aisling.BonusAc -= Template.AcModifer;
                            client.SendStats(StatusFlags.StructD);
                        }

                        if (Template.MrModifer != 0)
                        {
                            client.Aisling.BonusMr -= (byte)Template.MrModifer;
                        }

                        if (Template.HealthModifer != 0)
                        {
                            client.Aisling.BonusHp -= Template.HealthModifer;
                        }

                        if (Template.ManaModifer != 0)
                        {
                            client.Aisling.BonusMp -= Template.ManaModifer;
                        }

                        if (Template.RegenModifer != 0)
                        {
                            client.Aisling.BonusRegen -= Template.RegenModifer;
                        }

                        if (Template.StrModifer != 0)
                        {
                            client.Aisling.BonusStr -= Template.StrModifer;
                        }

                        if (Template.IntModifer != 0)
                        {
                            client.Aisling.BonusInt -= Template.IntModifer;
                        }

                        if (Template.WisModifer != 0)
                        {
                            client.Aisling.BonusWis -= Template.WisModifer;
                        }

                        if (Template.ConModifer != 0)
                        {
                            client.Aisling.BonusCon -= Template.ConModifer;
                        }

                        if (Template.DexModifer != 0)
                        {
                            client.Aisling.BonusDex -= Template.DexModifer;
                        }

                        if (Template.HitModifer != 0)
                        {
                            client.Aisling.BonusHit -= (byte)Template.HitModifer;
                        }

                        if (Template.DmgModifer != 0)
                        {
                            client.Aisling.BonusDmg -= (byte)Template.DmgModifer;
                        }
                    }
                    break;
            }
        }

        public void SpellLines(GameClient client, bool isPositive)
        {
            switch (isPositive)
            {
                case true:
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
                                        spell.Lines += Template.SpellLinesModifier;
                                        break;
                                    case 2:
                                        spell.Lines -= Template.SpellLinesModifier;
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
                                        spell.Lines += Template.SpellLinesModifier;
                                        break;
                                    case 2:
                                        spell.Lines -= Template.SpellLinesModifier;
                                        break;
                                }
                            }
                        }

                        if (spell.Lines > spell.Template.MaxLines)
                            spell.Lines = spell.Template.MaxLines;

                        UpdateSpellSlot(client, spell.Slot);
                    }
                    break;
                case false:
                    for (var i = 0; i < client.Aisling.SpellBook.Spells.Count; i++)
                    {
                        var spell = client.Aisling.SpellBook.FindInSlot(i);

                        if (spell?.Template == null) continue;
                        spell.Lines = spell.Template.BaseLines;

                        // Calculate Spell lines from off-hand first
                        if (client.Aisling.EquipmentManager.Equipment[3]?.Item != null)
                        {
                            var offHand = client.Aisling.EquipmentManager.Equipment[3].Item;

                            if (offHand.ItemId != ItemId)
                            {
                                var op = offHand.Template.IsPositiveSpellLines;

                                if (op != 0)
                                {
                                    switch (op)
                                    {
                                        case 1:
                                            spell.Lines += Template.SpellLinesModifier;
                                            break;
                                        case 2:
                                            spell.Lines -= Template.SpellLinesModifier;
                                            break;
                                    }
                                }
                            }
                        }

                        // Calculate Spell lines from weapon second
                        if (client.Aisling.EquipmentManager.Equipment[1]?.Item != null)
                        {
                            var weapon = client.Aisling.EquipmentManager.Equipment[1].Item;

                            if (weapon.ItemId != ItemId)
                            {
                                var op = weapon.Template.IsPositiveSpellLines;

                                if (op != 0)
                                {
                                    switch (op)
                                    {
                                        case 1:
                                            spell.Lines += Template.SpellLinesModifier;
                                            break;
                                        case 2:
                                            spell.Lines -= Template.SpellLinesModifier;
                                            break;
                                    }
                                }
                            }
                        }

                        if (spell.Lines > spell.Template.MaxLines)
                            spell.Lines = spell.Template.MaxLines;

                        UpdateSpellSlot(client, spell.Slot);
                    }
                    break;
            }
        }

        public void ItemVarianceCalc(GameClient client, bool isPositive)
        {
            switch (isPositive)
            {
                case true:
                    switch (ItemVariance)
                    {
                        case Variance.None:
                            break;
                        case Variance.Embunement:
                            client.Aisling.BonusHit += 5;
                            break;
                        case Variance.Blessing:
                            client.Aisling.BonusDmg += 2;
                            break;
                        case Variance.Mana:
                            client.Aisling.BonusMp += 250;
                            break;
                        case Variance.Gramail:
                            client.Aisling.BonusMr += 10;
                            break;
                        case Variance.Deoch:
                            client.Aisling.BonusRegen += 10;
                            client.Aisling.BonusAc -= 2;
                            break;
                        case Variance.Ceannlaidir:
                            client.Aisling.BonusStr += 1;
                            break;
                        case Variance.Cail:
                            client.Aisling.BonusCon += 1;
                            break;
                        case Variance.Fiosachd:
                            client.Aisling.BonusDex += 1;
                            break;
                        case Variance.Glioca:
                            client.Aisling.BonusWis += 1;
                            break;
                        case Variance.Luathas:
                            client.Aisling.BonusInt += 1;
                            break;
                        case Variance.Sgrios:
                            client.Aisling.BonusStr += 2;
                            client.Aisling.BonusAc += 1;
                            client.Aisling.BonusRegen -= 10;
                            break;
                        case Variance.Reinforcement:
                            client.Aisling.BonusAc += 1;
                            client.Aisling.BonusHp += 250;
                            break;
                        case Variance.Spikes:
                            client.Aisling.Spikes += 1;
                            break;
                    }
                    break;
                case false:
                    switch (ItemVariance)
                    {
                        case Variance.None:
                            break;
                        case Variance.Embunement:
                            client.Aisling.BonusHit -= 5;
                            break;
                        case Variance.Blessing:
                            client.Aisling.BonusDmg -= 2;
                            break;
                        case Variance.Mana:
                            client.Aisling.BonusMp -= 250;
                            break;
                        case Variance.Gramail:
                            client.Aisling.BonusMr -= 10;
                            break;
                        case Variance.Deoch:
                            client.Aisling.BonusRegen -= 10;
                            client.Aisling.BonusAc += 2;
                            break;
                        case Variance.Ceannlaidir:
                            client.Aisling.BonusStr -= 1;
                            break;
                        case Variance.Cail:
                            client.Aisling.BonusCon -= 1;
                            break;
                        case Variance.Fiosachd:
                            client.Aisling.BonusDex -= 1;
                            break;
                        case Variance.Glioca:
                            client.Aisling.BonusWis -= 1;
                            break;
                        case Variance.Luathas:
                            client.Aisling.BonusInt -= 1;
                            break;
                        case Variance.Sgrios:
                            client.Aisling.BonusStr -= 2;
                            client.Aisling.BonusAc -= 1;
                            client.Aisling.BonusRegen += 10;
                            break;
                        case Variance.Reinforcement:
                            client.Aisling.BonusAc -= 1;
                            client.Aisling.BonusHp -= 250;
                            break;
                        case Variance.Spikes:
                            client.Aisling.Spikes -= 1;
                            break;
                    }
                    break;
            }
        }

        public void WeaponVarianceCalc(GameClient client, bool isPositive)
        {
            switch (isPositive)
            {
                case true:
                    switch (WeapVariance)
                    {
                        case WeaponVariance.None:
                            break;
                        case WeaponVariance.Bleeding:
                            client.Aisling.Bleeding += 1;
                            break;
                        case WeaponVariance.Rending:
                            client.Aisling.Rending += 1;
                            break;
                        case WeaponVariance.Aegis:
                            client.Aisling.Aegis += 1;
                            break;
                        case WeaponVariance.Reaping:
                            client.Aisling.Reaping += 1;
                            break;
                        case WeaponVariance.Vampirism:
                            client.Aisling.Vampirism += 1;
                            break;
                        case WeaponVariance.Haste:
                            client.Aisling.Haste += 1;
                            break;
                        case WeaponVariance.Gust:
                            client.Aisling.Gust += 1;
                            break;
                        case WeaponVariance.Quake:
                            client.Aisling.Quake += 1;
                            break;
                        case WeaponVariance.Rain:
                            client.Aisling.Rain += 1;
                            break;
                        case WeaponVariance.Flame:
                            client.Aisling.Flame += 1;
                            break;
                        case WeaponVariance.Dusk:
                            client.Aisling.Dusk += 1;
                            break;
                        case WeaponVariance.Dawn:
                            client.Aisling.Dawn += 1;
                            break;
                    }
                    break;
                case false:
                    switch (WeapVariance)
                    {
                        case WeaponVariance.None:
                            break;
                        case WeaponVariance.Bleeding:
                            client.Aisling.Bleeding -= 1;
                            break;
                        case WeaponVariance.Rending:
                            client.Aisling.Rending -= 1;
                            break;
                        case WeaponVariance.Aegis:
                            client.Aisling.Aegis -= 1;
                            break;
                        case WeaponVariance.Reaping:
                            client.Aisling.Reaping -= 1;
                            break;
                        case WeaponVariance.Vampirism:
                            client.Aisling.Vampirism -= 1;
                            break;
                        case WeaponVariance.Haste:
                            client.Aisling.Haste -= 1;
                            break;
                        case WeaponVariance.Gust:
                            client.Aisling.Gust -= 1;
                            break;
                        case WeaponVariance.Quake:
                            client.Aisling.Quake -= 1;
                            break;
                        case WeaponVariance.Rain:
                            client.Aisling.Rain -= 1;
                            break;
                        case WeaponVariance.Flame:
                            client.Aisling.Flame -= 1;
                            break;
                        case WeaponVariance.Dusk:
                            client.Aisling.Dusk -= 1;
                            break;
                        case WeaponVariance.Dawn:
                            client.Aisling.Dawn -= 1;
                            break;
                    }
                    break;
            }
        }

        public void QualityVarianceCalc(GameClient client, bool isPositive)
        {
            switch (isPositive)
            {
                case true:
                    switch (ItemQuality)
                    {
                        case Quality.Damaged:
                            client.Aisling.BonusStr += 0;
                            client.Aisling.BonusInt += 0;
                            client.Aisling.BonusWis += 0;
                            client.Aisling.BonusCon += 0;
                            client.Aisling.BonusDex += 0;
                            client.Aisling.BonusDmg += 0;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc -= 2;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 0;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Common:
                            client.Aisling.BonusStr += 0;
                            client.Aisling.BonusInt += 0;
                            client.Aisling.BonusWis += 0;
                            client.Aisling.BonusCon += 0;
                            client.Aisling.BonusDex += 0;
                            client.Aisling.BonusDmg += 0;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc += 0;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 0;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Uncommon:
                            client.Aisling.BonusStr += 1;
                            client.Aisling.BonusInt += 1;
                            client.Aisling.BonusWis += 1;
                            client.Aisling.BonusCon += 1;
                            client.Aisling.BonusDex += 1;
                            client.Aisling.BonusDmg += 1;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc += 0;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 100;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Rare:
                            client.Aisling.BonusStr += 1;
                            client.Aisling.BonusInt += 1;
                            client.Aisling.BonusWis += 1;
                            client.Aisling.BonusCon += 1;
                            client.Aisling.BonusDex += 1;
                            client.Aisling.BonusDmg += 2;
                            client.Aisling.BonusHit += 5;
                            client.Aisling.BonusAc += 1;
                            client.Aisling.BonusMr += 5;
                            client.Aisling.BonusHp += 500;
                            client.Aisling.BonusMp += 100;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Epic:
                            client.Aisling.BonusStr += 2;
                            client.Aisling.BonusInt += 2;
                            client.Aisling.BonusWis += 2;
                            client.Aisling.BonusCon += 2;
                            client.Aisling.BonusDex += 2;
                            client.Aisling.BonusDmg += 2;
                            client.Aisling.BonusHit += 10;
                            client.Aisling.BonusAc += 1;
                            client.Aisling.BonusMr += 5;
                            client.Aisling.BonusHp += 750;
                            client.Aisling.BonusMp += 250;
                            client.Aisling.BonusRegen += 5;
                            break;
                        case Quality.Legendary:
                            client.Aisling.BonusStr += 3;
                            client.Aisling.BonusInt += 3;
                            client.Aisling.BonusWis += 3;
                            client.Aisling.BonusCon += 3;
                            client.Aisling.BonusDex += 3;
                            client.Aisling.BonusDmg += 15;
                            client.Aisling.BonusHit += 20;
                            client.Aisling.BonusAc += 2;
                            client.Aisling.BonusMr += 10;
                            client.Aisling.BonusHp += 1000;
                            client.Aisling.BonusMp += 500;
                            client.Aisling.BonusRegen += 10;
                            break;
                        case Quality.Forsaken:
                            client.Aisling.BonusStr += 4;
                            client.Aisling.BonusInt += 4;
                            client.Aisling.BonusWis += 4;
                            client.Aisling.BonusCon += 4;
                            client.Aisling.BonusDex += 4;
                            client.Aisling.BonusDmg += 20;
                            client.Aisling.BonusHit += 25;
                            client.Aisling.BonusAc += 3;
                            client.Aisling.BonusMr += 10;
                            client.Aisling.BonusHp += 1500;
                            client.Aisling.BonusMp += 1000;
                            client.Aisling.BonusRegen += 20;
                            break;
                        case Quality.Mythic:
                            client.Aisling.BonusStr += 5;
                            client.Aisling.BonusInt += 5;
                            client.Aisling.BonusWis += 5;
                            client.Aisling.BonusCon += 5;
                            client.Aisling.BonusDex += 5;
                            client.Aisling.BonusDmg += 25;
                            client.Aisling.BonusHit += 30;
                            client.Aisling.BonusAc += 5;
                            client.Aisling.BonusMr += 20;
                            client.Aisling.BonusHp += 2500;
                            client.Aisling.BonusMp += 2000;
                            client.Aisling.BonusRegen += 40;
                            break;
                        default:
                            client.Aisling.BonusStr += 0;
                            client.Aisling.BonusInt += 0;
                            client.Aisling.BonusWis += 0;
                            client.Aisling.BonusCon += 0;
                            client.Aisling.BonusDex += 0;
                            client.Aisling.BonusDmg += 0;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc += 0;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 0;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                    }
                    break;
                case false:
                    switch (ItemQuality)
                    {
                        case Quality.Damaged:
                            client.Aisling.BonusStr += 0;
                            client.Aisling.BonusInt += 0;
                            client.Aisling.BonusWis += 0;
                            client.Aisling.BonusCon += 0;
                            client.Aisling.BonusDex += 0;
                            client.Aisling.BonusDmg += 0;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc += 2;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 0;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Common:
                            client.Aisling.BonusStr += 0;
                            client.Aisling.BonusInt += 0;
                            client.Aisling.BonusWis += 0;
                            client.Aisling.BonusCon += 0;
                            client.Aisling.BonusDex += 0;
                            client.Aisling.BonusDmg += 0;
                            client.Aisling.BonusHit += 0;
                            client.Aisling.BonusAc += 0;
                            client.Aisling.BonusMr += 0;
                            client.Aisling.BonusHp += 0;
                            client.Aisling.BonusMp += 0;
                            client.Aisling.BonusRegen += 0;
                            break;
                        case Quality.Uncommon:
                            client.Aisling.BonusStr -= 1;
                            client.Aisling.BonusInt -= 1;
                            client.Aisling.BonusWis -= 1;
                            client.Aisling.BonusCon -= 1;
                            client.Aisling.BonusDex -= 1;
                            client.Aisling.BonusDmg -= 1;
                            client.Aisling.BonusHit -= 0;
                            client.Aisling.BonusAc -= 0;
                            client.Aisling.BonusMr -= 0;
                            client.Aisling.BonusHp -= 100;
                            client.Aisling.BonusMp -= 0;
                            client.Aisling.BonusRegen -= 0;
                            break;
                        case Quality.Rare:
                            client.Aisling.BonusStr -= 1;
                            client.Aisling.BonusInt -= 1;
                            client.Aisling.BonusWis -= 1;
                            client.Aisling.BonusCon -= 1;
                            client.Aisling.BonusDex -= 1;
                            client.Aisling.BonusDmg -= 2;
                            client.Aisling.BonusHit -= 5;
                            client.Aisling.BonusAc -= 1;
                            client.Aisling.BonusMr -= 5;
                            client.Aisling.BonusHp -= 500;
                            client.Aisling.BonusMp -= 100;
                            client.Aisling.BonusRegen -= 0;
                            break;
                        case Quality.Epic:
                            client.Aisling.BonusStr -= 2;
                            client.Aisling.BonusInt -= 2;
                            client.Aisling.BonusWis -= 2;
                            client.Aisling.BonusCon -= 2;
                            client.Aisling.BonusDex -= 2;
                            client.Aisling.BonusDmg -= 2;
                            client.Aisling.BonusHit -= 10;
                            client.Aisling.BonusAc -= 1;
                            client.Aisling.BonusMr -= 5;
                            client.Aisling.BonusHp -= 750;
                            client.Aisling.BonusMp -= 250;
                            client.Aisling.BonusRegen -= 5;
                            break;
                        case Quality.Legendary:
                            client.Aisling.BonusStr -= 3;
                            client.Aisling.BonusInt -= 3;
                            client.Aisling.BonusWis -= 3;
                            client.Aisling.BonusCon -= 3;
                            client.Aisling.BonusDex -= 3;
                            client.Aisling.BonusDmg -= 15;
                            client.Aisling.BonusHit -= 20;
                            client.Aisling.BonusAc -= 2;
                            client.Aisling.BonusMr -= 10;
                            client.Aisling.BonusHp -= 1000;
                            client.Aisling.BonusMp -= 500;
                            client.Aisling.BonusRegen -= 10;
                            break;
                        case Quality.Forsaken:
                            client.Aisling.BonusStr -= 4;
                            client.Aisling.BonusInt -= 4;
                            client.Aisling.BonusWis -= 4;
                            client.Aisling.BonusCon -= 4;
                            client.Aisling.BonusDex -= 4;
                            client.Aisling.BonusDmg -= 20;
                            client.Aisling.BonusHit -= 25;
                            client.Aisling.BonusAc -= 3;
                            client.Aisling.BonusMr -= 10;
                            client.Aisling.BonusHp -= 1500;
                            client.Aisling.BonusMp -= 1000;
                            client.Aisling.BonusRegen -= 20;
                            break;
                        case Quality.Mythic:
                            client.Aisling.BonusStr -= 5;
                            client.Aisling.BonusInt -= 5;
                            client.Aisling.BonusWis -= 5;
                            client.Aisling.BonusCon -= 5;
                            client.Aisling.BonusDex -= 5;
                            client.Aisling.BonusDmg -= 25;
                            client.Aisling.BonusHit -= 30;
                            client.Aisling.BonusAc -= 5;
                            client.Aisling.BonusMr -= 20;
                            client.Aisling.BonusHp -= 2500;
                            client.Aisling.BonusMp -= 2000;
                            client.Aisling.BonusRegen -= 40;
                            break;
                        default:
                            client.Aisling.BonusStr -= 0;
                            client.Aisling.BonusInt -= 0;
                            client.Aisling.BonusWis -= 0;
                            client.Aisling.BonusCon -= 0;
                            client.Aisling.BonusDex -= 0;
                            client.Aisling.BonusDmg -= 0;
                            client.Aisling.BonusHit -= 0;
                            client.Aisling.BonusAc -= 0;
                            client.Aisling.BonusMr -= 0;
                            client.Aisling.BonusHp -= 0;
                            client.Aisling.BonusMp -= 0;
                            client.Aisling.BonusRegen -= 0;
                            break;
                    }
                    break;
            }
        }

        public void UpdateSpellSlot(GameClient client, byte slot)
        {
            var a = client.Aisling.SpellBook.Remove(slot);
            client.Send(new ServerFormat18(slot));

            if (a == null) return;
            a.Slot = slot;
            client.Aisling.SpellBook.Set(a);
            client.Send(new ServerFormat17(a));
        }
    }
}