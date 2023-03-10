using Darkages.Enums;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;

using static Darkages.Enums.ElementManager;

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
    public double Weight { get => DropRate; set { } }
    public ItemFlags Flags { get; init; }
    public uint MaxDurability { get; set; }
    public uint Value { get; set; }
    public int EquipmentSlot { get; set; }
    public string NpcKey { get; init; }
    public Class Class { get; init; }
    public uint LevelRequired { get; init; }
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
        var category = string.IsNullOrEmpty(Group) ? string.Empty : Group;

        if (string.IsNullOrEmpty(category)) category = Class == Class.Peasant ? "Weapons" : Class.ToString();

        var classConvert = ((int)Class).ToString();

        classConvert = classConvert switch
        {
            "0" => "0",
            "1" => "1",
            "2" => "1",
            "3" => "2",
            "4" => "4",
            "5" => "3",
            "6" => "5",
            _ => classConvert
        };

        if (Gender == 0)
        {
            Gender = Gender.Both;
        }

        return new[]
        {
            LevelRequired.ToString(),
            classConvert,
            CarryWeight.ToString(),

            Gender switch
            {
                Gender.Both => category,
                Gender.Female => category,
                Gender.Male => category,
                _ => category
            },
            Gender switch
            {
                Gender.Both => category,
                Gender.Female => category,
                Gender.Male => category,
                _ => category
            } + $" {Gender}\n{Description}"
        };
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
                    LevelRequired = (uint)level,
                    DropRate = (double)drop,
                    StageRequired = classStage,
                    Color = color,
                    Name = reader["Name"].ToString(),
                    Group = reader["GroupIn"].ToString()
                };

                if (temp.Name == null) continue;
                ServerSetup.Instance.GlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
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
                    LevelRequired = (uint)level,
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
                ServerSetup.Instance.GlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
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
                    LevelRequired = (uint)level,
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
                ServerSetup.Instance.GlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
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
                    LevelRequired = (uint)level,
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
                ServerSetup.Instance.GlobalItemTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}