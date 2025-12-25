using Darkages.Sprites.Entity;

namespace Darkages.Enums;

public static class ItemEnumConverters
{
    public static string QualityToString(Item.Quality e)
    {
        return e switch
        {
            Item.Quality.Damaged => "Damaged",
            Item.Quality.Common => "Common",
            Item.Quality.Uncommon => "Uncommon",
            Item.Quality.Rare => "Rare",
            Item.Quality.Epic => "Epic",
            Item.Quality.Legendary => "Legendary",
            Item.Quality.Forsaken => "Forsaken",
            Item.Quality.Mythic => "Mythic",
            Item.Quality.Primordial => "Primordial",
            Item.Quality.Transcendent => "Transcendent",
            _ => "Common"
        };
    }

    public static Item.Quality StringToQuality(string? value)
    {
        return value switch
        {
            "Damaged" => Item.Quality.Damaged,
            "Common" => Item.Quality.Common,
            "Uncommon" => Item.Quality.Uncommon,
            "Rare" => Item.Quality.Rare,
            "Epic" => Item.Quality.Epic,
            "Legendary" => Item.Quality.Legendary,
            "Forsaken" => Item.Quality.Forsaken,
            "Mythic" => Item.Quality.Mythic,
            "Primordial" => Item.Quality.Primordial,
            "Transcendent" => Item.Quality.Transcendent,
            _ => Item.Quality.Common
        };
    }

    public static string PaneToString(Item.ItemPanes e)
    {
        return e switch
        {
            Item.ItemPanes.Ground => "Ground",
            Item.ItemPanes.Inventory => "Inventory",
            Item.ItemPanes.Equip => "Equip",
            Item.ItemPanes.Bank => "Bank",
            Item.ItemPanes.Archived => "Archived",
            _ => "Ground"
        };
    }

    public static Item.ItemPanes StringToPane(string? value)
    {
        return value switch
        {
            "Ground" => Item.ItemPanes.Ground,
            "Inventory" => Item.ItemPanes.Inventory,
            "Equip" => Item.ItemPanes.Equip,
            "Bank" => Item.ItemPanes.Bank,
            "Archived" => Item.ItemPanes.Archived,
            _ => Item.ItemPanes.Ground
        };
    }

    public static string ArmorVarianceToString(Item.Variance e)
    {
        return e switch
        {
            Item.Variance.None => "None",
            Item.Variance.Embunement => "Embunement",
            Item.Variance.Blessing => "Blessing",
            Item.Variance.Mana => "Mana",
            Item.Variance.Gramail => "Gramail",
            Item.Variance.Deoch => "Deoch",
            Item.Variance.Ceannlaidir => "Ceannlaidir",
            Item.Variance.Cail => "Cail",
            Item.Variance.Fiosachd => "Fiosachd",
            Item.Variance.Glioca => "Glioca",
            Item.Variance.Luathas => "Luathas",
            Item.Variance.Sgrios => "Sgrios",
            Item.Variance.Reinforcement => "Reinforcement",
            Item.Variance.Spikes => "Spikes",
            _ => "None"
        };
    }

    public static Item.Variance StringToArmorVariance(string? value)
    {
        return value switch
        {
            "None" => Item.Variance.None,
            "Embunement" => Item.Variance.Embunement,
            "Blessing" => Item.Variance.Blessing,
            "Mana" => Item.Variance.Mana,
            "Gramail" => Item.Variance.Gramail,
            "Deoch" => Item.Variance.Deoch,
            "Ceannlaidir" => Item.Variance.Ceannlaidir,
            "Cail" => Item.Variance.Cail,
            "Fiosachd" => Item.Variance.Fiosachd,
            "Glioca" => Item.Variance.Glioca,
            "Luathas" => Item.Variance.Luathas,
            "Sgrios" => Item.Variance.Sgrios,
            "Reinforcement" => Item.Variance.Reinforcement,
            "Spikes" => Item.Variance.Spikes,
            _ => Item.Variance.None
        };
    }

    public static string WeaponVarianceToString(Item.WeaponVariance e)
    {
        return e switch
        {
            Item.WeaponVariance.None => "None",
            Item.WeaponVariance.Bleeding => "Bleeding",
            Item.WeaponVariance.Rending => "Rending",
            Item.WeaponVariance.Aegis => "Aegis",
            Item.WeaponVariance.Reaping => "Reaping",
            Item.WeaponVariance.Vampirism => "Vampirism",
            Item.WeaponVariance.Ghosting => "Ghosting",
            Item.WeaponVariance.Haste => "Haste",
            Item.WeaponVariance.Gust => "Gust",
            Item.WeaponVariance.Quake => "Quake",
            Item.WeaponVariance.Rain => "Rain",
            Item.WeaponVariance.Flame => "Flame",
            Item.WeaponVariance.Dusk => "Dusk",
            Item.WeaponVariance.Dawn => "Dawn",
            _ => "None"
        };
    }

    public static Item.WeaponVariance StringToWeaponVariance(string? value)
    {
        return value switch
        {
            "None" => Item.WeaponVariance.None,
            "Bleeding" => Item.WeaponVariance.Bleeding,
            "Rending" => Item.WeaponVariance.Rending,
            "Aegis" => Item.WeaponVariance.Aegis,
            "Reaping" => Item.WeaponVariance.Reaping,
            "Vampirism" => Item.WeaponVariance.Vampirism,
            "Ghosting" => Item.WeaponVariance.Ghosting,
            "Haste" => Item.WeaponVariance.Haste,
            "Gust" => Item.WeaponVariance.Gust,
            "Quake" => Item.WeaponVariance.Quake,
            "Rain" => Item.WeaponVariance.Rain,
            "Flame" => Item.WeaponVariance.Flame,
            "Dusk" => Item.WeaponVariance.Dusk,
            "Dawn" => Item.WeaponVariance.Dawn,
            _ => Item.WeaponVariance.None
        };
    }

    public static string GearEnhancementToString(Item.GearEnhancements e)
    {
        return e switch
        {
            Item.GearEnhancements.None => "None",
            Item.GearEnhancements.One => "One",
            Item.GearEnhancements.Two => "Two",
            Item.GearEnhancements.Three => "Three",
            Item.GearEnhancements.Four => "Four",
            Item.GearEnhancements.Five => "Five",
            Item.GearEnhancements.Six => "Six",
            _ => "None"
        };
    }

    public static Item.GearEnhancements StringToGearEnhancement(string? value)
    {
        return value switch
        {
            "None" => Item.GearEnhancements.None,
            "One" => Item.GearEnhancements.One,
            "Two" => Item.GearEnhancements.Two,
            "Three" => Item.GearEnhancements.Three,
            "Four" => Item.GearEnhancements.Four,
            "Five" => Item.GearEnhancements.Five,
            "Six" => Item.GearEnhancements.Six,
            _ => Item.GearEnhancements.None
        };
    }

    public static string ItemMaterialToString(Item.ItemMaterials e)
    {
        return e switch
        {
            Item.ItemMaterials.None => "None",
            Item.ItemMaterials.Copper => "Copper",
            Item.ItemMaterials.Iron => "Iron",
            Item.ItemMaterials.Steel => "Steel",
            Item.ItemMaterials.Forged => "Forged",
            Item.ItemMaterials.Elven => "Elven",
            Item.ItemMaterials.Dwarven => "Dwarven",
            Item.ItemMaterials.Mythril => "Mythril",
            Item.ItemMaterials.Hybrasyl => "Hybrasyl",
            Item.ItemMaterials.Ebony => "Ebony",
            Item.ItemMaterials.Chaos => "Chaos",
            Item.ItemMaterials.MoonStone => "MoonStone",
            Item.ItemMaterials.SunStone => "SunStone",
            Item.ItemMaterials.Runic => "Runic",
            _ => "None"
        };
    }

    public static Item.ItemMaterials StringToItemMaterial(string? value)
    {
        return value switch
        {
            "None" => Item.ItemMaterials.None,
            "Copper" => Item.ItemMaterials.Copper,
            "Iron" => Item.ItemMaterials.Iron,
            "Steel" => Item.ItemMaterials.Steel,
            "Forged" => Item.ItemMaterials.Forged,
            "Elven" => Item.ItemMaterials.Elven,
            "Dwarven" => Item.ItemMaterials.Dwarven,
            "Mythril" => Item.ItemMaterials.Mythril,
            "Hybrasyl" => Item.ItemMaterials.Hybrasyl,
            "Ebony" => Item.ItemMaterials.Ebony,
            "Chaos" => Item.ItemMaterials.Chaos,
            "MoonStone" => Item.ItemMaterials.MoonStone,
            "SunStone" => Item.ItemMaterials.SunStone,
            "Runic" => Item.ItemMaterials.Runic,
            _ => Item.ItemMaterials.None
        };
    }
}