using Dapper;

using Darkages.Database;
using Darkages.Enums;
using Microsoft.Data.SqlClient;

using System.Data;
using Darkages.Types;
using Element = Darkages.Enums.ElementManager.Element;
using Gender = Darkages.Enums.Gender;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.Templates;

public class ItemTemplate : Template
{
    public bool CanStack { get; init; }
    public byte MaxStack { get; init; }
    public ushort Image { get; init; }
    public ushort OffHandImage { get; init; }
    public ushort DisplayImage { get; init; }
    public string ScriptName { get; init; }
    public Gender Gender { get; set; }
    public int HealthModifer { get; init; }
    public int ManaModifer { get; init; }
    public int StrModifer { get; init; }
    public int IntModifer { get; init; }
    public int WisModifer { get; init; }
    public int ConModifer { get; init; }
    public int DexModifer { get; init; }
    public int AcModifer { get; init; }
    public int MrModifer { get; init; }
    public int HitModifer { get; init; }
    public int DmgModifer { get; init; }
    public int RegenModifer { get; init; }
    public int SpellLinesModifier { get; init; }
    public int IsPositiveSpellLines { get; init; }
    public int SpellMinValue { get; set; }
    public int SpellMaxValue { get; set; }
    public Element DefenseElement { get; init; }
    public Element SecondaryDefensiveElement { get; init; }
    public Element OffenseElement { get; init; }
    public Element SecondaryOffensiveElement { get; init; }
    public byte CarryWeight { get; init; }
    public ItemFlags Flags { get; init; }
    public uint MaxDurability { get; init; }
    public uint Value { get; init; }
    public int EquipmentSlot { get; set; }
    public string NpcKey { get; init; }
    public Class Class { get; init; }
    public ushort LevelRequired { get; init; }
    public Job JobRequired { get; init; } = Job.None;
    public ushort JobLevelRequired { get; init; }
    public ClassStage StageRequired { get; init; }
    public int DmgMin { get; init; }
    public int DmgMax { get; init; }
    public double DropRate { get; init; }
    public bool HasPants { get; init; }
    public ItemColor Color { get; init; }
    public string WeaponScript { get; init; }
    public bool Enchantable { get; init; }

    public string[] GetMetaData()
    {
        var category = string.IsNullOrEmpty(Group) ? "Other" : Group;

        if (category == "Other" && EquipmentSlot is 0 or 2)
            category = Class.ToString();

        if (category == "Other" && EquipmentSlot != 0)
            category = ItemSlots.ItemSlotMetaValuesStoresBank(EquipmentSlot);

        if (Gender == 0)
            Gender = Gender.Unisex;

        var stage = 0;
        if (StageRequired >= ClassStage.Master)
            stage = 1;

        return
        [
            $"{LevelRequired}/{stage}/{JobLevelRequired}\n",
            $"{ClassStrings.ItemClassToIntMetaData(Class.ToString())}\n",
            $"{CarryWeight}\n",
            category,
            $"{Gender}\n{Description}"
        ];
    }
}

public static class ItemStorage
{
    public static void CacheFromDatabaseConsumables(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM Zolian.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var max = (int)reader["MaxStack"];
                var image = (int)reader["Image"];
                var disImage = (int)reader["DisplayImage"];
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<ItemFlags>(reader["Flags"]);
                var weight = (int)reader["CarryWeight"];
                var worth = (int)reader["Worth"];
                var itemClass = ServiceStack.AutoMappingUtils.ConvertTo<Class>(reader["Class"]);
                var level = (int)reader["LevelRequired"];
                var jobLevel = (int)reader["JobLevelRequired"];
                var drop = (decimal)reader["DropRate"];
                var classStage = ServiceStack.AutoMappingUtils.ConvertTo<ClassStage>(reader["StageRequired"]);
                var color = ServiceStack.AutoMappingUtils.ConvertTo<ItemColor>(reader["Color"]);
                var temp = new ItemTemplate
                {
                    CanStack = (bool)reader["CanStack"],
                    MaxStack = (byte)max,
                    Image = (ushort)image,
                    DisplayImage = (ushort)disImage,
                    ScriptName = reader["ScriptName"].ToString(),
                    Flags = flags,
                    CarryWeight = (byte)weight,
                    Value = (uint)worth,
                    NpcKey = reader["NpcKey"].ToString(),
                    Class = itemClass,
                    LevelRequired = (ushort)level,
                    JobLevelRequired = (ushort)jobLevel,
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    Color = color,
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString()
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void CacheFromDatabaseArmor(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM Zolian.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var image = (int)reader["Image"];
                var disImage = (int)reader["DisplayImage"];
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<ItemFlags>(reader["Flags"]);
                var gender = ServiceStack.AutoMappingUtils.ConvertTo<Gender>(reader["Gender"]);
                var weight = (int)reader["CarryWeight"];
                var maxDura = (int)reader["MaxDurability"];
                var worth = (int)reader["Worth"];
                var itemClass = ServiceStack.AutoMappingUtils.ConvertTo<Class>(reader["Class"]);
                var level = (int)reader["LevelRequired"];
                var jobLevel = (int)reader["JobLevelRequired"];
                var drop = (decimal)reader["DropRate"];
                var classStage = ServiceStack.AutoMappingUtils.ConvertTo<ClassStage>(reader["StageRequired"]);
                var color = ServiceStack.AutoMappingUtils.ConvertTo<ItemColor>(reader["Color"]);
                var temp = new ItemTemplate
                {
                    Image = (ushort)image,
                    DisplayImage = (ushort)disImage,
                    ScriptName = reader["ScriptName"].ToString(),
                    Flags = flags,
                    Gender = gender,
                    OffenseElement = Element.None,
                    DefenseElement = Element.None,
                    CarryWeight = (byte)weight,
                    Enchantable = (bool)reader["Enchantable"],
                    MaxDurability = (uint)maxDura,
                    Value = (uint)worth,
                    EquipmentSlot = (int)reader["EquipmentSlot"],
                    NpcKey = "",
                    Class = itemClass,
                    LevelRequired = (ushort)level,
                    JobLevelRequired = (ushort)jobLevel,
                    DmgMin = 0,
                    DmgMax = 0,
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    HasPants = (bool)reader["HasPants"],
                    Color = color,
                    WeaponScript = reader["ArmorScript"].ToString(),
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString(),
                    HealthModifer = (int)reader["HP"],
                    ManaModifer = (int)reader["MP"],
                    AcModifer = (int)reader["ArmorClass"],
                    StrModifer = (int)reader["Strength"],
                    IntModifer = (int)reader["Intelligence"],
                    WisModifer = (int)reader["Wisdom"],
                    ConModifer = (int)reader["Constitution"],
                    DexModifer = (int)reader["Dexterity"],
                    MrModifer = (int)reader["MagicResistance"],
                    HitModifer = (int)reader["Hit"],
                    DmgModifer = (int)reader["Dmg"],
                    RegenModifer = (int)reader["Regen"],
                    SpellLinesModifier = 0,
                    IsPositiveSpellLines = 0,
                    SpellMinValue = 0,
                    SpellMaxValue = 0
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void CacheFromDatabaseEquipment(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM Zolian.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var image = (int)reader["Image"];
                var disImage = (int)reader["DisplayImage"];
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<ItemFlags>(reader["Flags"]);
                var gender = ServiceStack.AutoMappingUtils.ConvertTo<Gender>(reader["Gender"]);
                var offEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["OffenseElement"]);
                var defEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["DefenseElement"]);
                var weight = (int)reader["CarryWeight"];
                var maxDura = (int)reader["MaxDurability"];
                var worth = (int)reader["Worth"];
                var itemClass = ServiceStack.AutoMappingUtils.ConvertTo<Class>(reader["Class"]);
                var level = (int)reader["LevelRequired"];
                var jobLevel = (int)reader["JobLevelRequired"];
                var drop = (decimal)reader["DropRate"];
                var classStage = ServiceStack.AutoMappingUtils.ConvertTo<ClassStage>(reader["StageRequired"]);
                var color = ServiceStack.AutoMappingUtils.ConvertTo<ItemColor>(reader["Color"]);
                var temp = new ItemTemplate
                {
                    Image = (ushort)image,
                    DisplayImage = (ushort)disImage,
                    ScriptName = reader["ScriptName"].ToString(),
                    Flags = flags,
                    Gender = gender,
                    OffenseElement = offEle,
                    DefenseElement = defEle,
                    CarryWeight = (byte)weight,
                    Enchantable = (bool)reader["Enchantable"],
                    MaxDurability = (uint)maxDura,
                    Value = (uint)worth,
                    EquipmentSlot = (int)reader["EquipmentSlot"],
                    NpcKey = "",
                    Class = itemClass,
                    LevelRequired = (ushort)level,
                    JobLevelRequired = (ushort)jobLevel,
                    DmgMin = 0,
                    DmgMax = 0,
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    HasPants = false,
                    Color = color,
                    WeaponScript = reader["EquipmentScript"].ToString(),
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString(),
                    HealthModifer = (int)reader["HP"],
                    ManaModifer = (int)reader["MP"],
                    AcModifer = (int)reader["ArmorClass"],
                    StrModifer = (int)reader["Strength"],
                    IntModifer = (int)reader["Intelligence"],
                    WisModifer = (int)reader["Wisdom"],
                    ConModifer = (int)reader["Constitution"],
                    DexModifer = (int)reader["Dexterity"],
                    MrModifer = (int)reader["MagicResistance"],
                    HitModifer = (int)reader["Hit"],
                    DmgModifer = (int)reader["Dmg"],
                    RegenModifer = (int)reader["Regen"],
                    SpellLinesModifier = 0,
                    IsPositiveSpellLines = 0,
                    SpellMinValue = 0,
                    SpellMaxValue = 0
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void CacheFromDatabaseOffense(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM Zolian.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var image = (int)reader["Image"];
                var offhand = (int)reader["OffHandImage"];
                var disImage = (int)reader["DisplayImage"];
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<ItemFlags>(reader["Flags"]);
                var gender = ServiceStack.AutoMappingUtils.ConvertTo<Gender>(reader["Gender"]);
                var offEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["SecondaryOffensiveElement"]);
                var defEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["SecondaryDefensiveElement"]);
                var weight = (int)reader["CarryWeight"];
                var maxDura = (int)reader["MaxDurability"];
                var worth = (int)reader["Worth"];
                var itemClass = ServiceStack.AutoMappingUtils.ConvertTo<Class>(reader["Class"]);
                var level = (int)reader["LevelRequired"];
                var jobLevel = (int)reader["JobLevelRequired"];
                var drop = (decimal)reader["DropRate"];
                var classStage = ServiceStack.AutoMappingUtils.ConvertTo<ClassStage>(reader["StageRequired"]);
                var color = ServiceStack.AutoMappingUtils.ConvertTo<ItemColor>(reader["Color"]);
                var temp = new ItemTemplate
                {
                    Image = (ushort)image,
                    OffHandImage = (ushort)offhand,
                    DisplayImage = (ushort)disImage,
                    ScriptName = reader["ScriptName"].ToString(),
                    Flags = flags,
                    Gender = gender,
                    SecondaryOffensiveElement = offEle,
                    SecondaryDefensiveElement = defEle,
                    CarryWeight = (byte)weight,
                    Enchantable = (bool)reader["Enchantable"],
                    MaxDurability = (uint)maxDura,
                    Value = (uint)worth,
                    EquipmentSlot = (int)reader["EquipmentSlot"],
                    NpcKey = reader["NpcKey"].ToString(),
                    Class = itemClass,
                    LevelRequired = (ushort)level,
                    JobLevelRequired = (ushort)jobLevel,
                    DmgMin = (int)reader["DmgMin"],
                    DmgMax = (int)reader["DmgMax"],
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    HasPants = (bool)reader["HasPants"],
                    Color = color,
                    WeaponScript = reader["WeaponScript"].ToString(),
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString(),
                    HealthModifer = (int)reader["HP"],
                    ManaModifer = (int)reader["MP"],
                    AcModifer = (int)reader["ArmorClass"],
                    StrModifer = (int)reader["Strength"],
                    IntModifer = (int)reader["Intelligence"],
                    WisModifer = (int)reader["Wisdom"],
                    ConModifer = (int)reader["Constitution"],
                    DexModifer = (int)reader["Dexterity"],
                    MrModifer = (int)reader["MagicResistance"],
                    HitModifer = (int)reader["Hit"],
                    DmgModifer = (int)reader["Dmg"],
                    RegenModifer = (int)reader["Regen"],
                    SpellLinesModifier = (int)reader["SpellLinesModifier"],
                    IsPositiveSpellLines = (int)reader["IsPositiveSpellLines"],
                    SpellMinValue = (int)reader["SpellMinValue"],
                    SpellMaxValue = (int)reader["SpellMaxValue"]
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void CacheFromDatabaseOffenseJobs(string conn)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM Zolian.dbo.WeaponJobs";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var image = (int)reader["Image"];
                var offhand = (int)reader["OffHandImage"];
                var disImage = (int)reader["DisplayImage"];
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<ItemFlags>(reader["Flags"]);
                var gender = ServiceStack.AutoMappingUtils.ConvertTo<Gender>(reader["Gender"]);
                var offEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["SecondaryOffensiveElement"]);
                var defEle = ServiceStack.AutoMappingUtils.ConvertTo<Element>(reader["SecondaryDefensiveElement"]);
                var weight = (int)reader["CarryWeight"];
                var maxDura = (int)reader["MaxDurability"];
                var worth = (int)reader["Worth"];
                var itemClass = ServiceStack.AutoMappingUtils.ConvertTo<Class>(reader["Class"]);
                var level = (int)reader["LevelRequired"];
                var job = ServiceStack.AutoMappingUtils.ConvertTo<Job>(reader["JobRequired"]);
                var jobLevel = (int)reader["JobLevelRequired"];
                var drop = (decimal)reader["DropRate"];
                var classStage = ServiceStack.AutoMappingUtils.ConvertTo<ClassStage>(reader["StageRequired"]);
                var color = ServiceStack.AutoMappingUtils.ConvertTo<ItemColor>(reader["Color"]);
                var temp = new ItemTemplate
                {
                    Image = (ushort)image,
                    OffHandImage = (ushort)offhand,
                    DisplayImage = (ushort)disImage,
                    ScriptName = reader["ScriptName"].ToString(),
                    Flags = flags,
                    Gender = gender,
                    SecondaryOffensiveElement = offEle,
                    SecondaryDefensiveElement = defEle,
                    CarryWeight = (byte)weight,
                    Enchantable = (bool)reader["Enchantable"],
                    MaxDurability = (uint)maxDura,
                    Value = (uint)worth,
                    EquipmentSlot = (int)reader["EquipmentSlot"],
                    NpcKey = reader["NpcKey"].ToString(),
                    Class = itemClass,
                    LevelRequired = (ushort)level,
                    JobRequired = job,
                    JobLevelRequired = (ushort)jobLevel,
                    DmgMin = (int)reader["DmgMin"],
                    DmgMax = (int)reader["DmgMax"],
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    HasPants = (bool)reader["HasPants"],
                    Color = color,
                    WeaponScript = reader["WeaponScript"].ToString(),
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString(),
                    HealthModifer = (int)reader["HP"],
                    ManaModifer = (int)reader["MP"],
                    AcModifer = (int)reader["ArmorClass"],
                    StrModifer = (int)reader["Strength"],
                    IntModifer = (int)reader["Intelligence"],
                    WisModifer = (int)reader["Wisdom"],
                    ConModifer = (int)reader["Constitution"],
                    DexModifer = (int)reader["Dexterity"],
                    MrModifer = (int)reader["MagicResistance"],
                    HitModifer = (int)reader["Hit"],
                    DmgModifer = (int)reader["Dmg"],
                    RegenModifer = (int)reader["Regen"],
                    SpellLinesModifier = (int)reader["SpellLinesModifier"],
                    IsPositiveSpellLines = (int)reader["IsPositiveSpellLines"],
                    SpellMinValue = (int)reader["SpellMinValue"],
                    SpellMaxValue = (int)reader["SpellMaxValue"]
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void PlayerItemsDbToCache()
    {
        try
        {
            // Clear cache prior to load
            ServerSetup.Instance.GlobalSqlItemCache.Clear();

            const string procedure = "[LoadItemsToCache]";
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var itemList = sConn.Query<Item>(procedure, commandType: CommandType.StoredProcedure);

            foreach (var item in itemList)
            {
                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;

                var itemName = item.Name;
                var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                {
                    item.Template = template;
                }

                var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                var newItem = new Item
                {
                    ItemId = item.ItemId,
                    Template = item.Template,
                    Name = itemName,
                    Serial = item.Serial,
                    ItemPane = item.ItemPane,
                    Slot = item.Slot,
                    InventorySlot = item.InventorySlot,
                    Color = color,
                    Durability = item.Durability,
                    Identified = item.Identified,
                    ItemVariance = item.ItemVariance,
                    WeapVariance = item.WeapVariance,
                    ItemQuality = item.ItemQuality,
                    OriginalQuality = item.OriginalQuality,
                    Stacks = item.Stacks,
                    Enchantable = item.Template.Enchantable,
                    Tarnished = item.Tarnished,
                    GearEnhancement = item.GearEnhancement,
                    ItemMaterial = item.ItemMaterial,
                    Image = item.Template.Image,
                    DisplayImage = item.Template.DisplayImage
                };

                newItem.GetDisplayName();
                newItem.NoColorGetDisplayName();
                ServerSetup.Instance.GlobalSqlItemCache.TryAdd(newItem.ItemId, newItem);
            }

            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }
}