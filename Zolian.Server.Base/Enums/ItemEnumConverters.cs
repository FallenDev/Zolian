using Darkages.Sprites;

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
}