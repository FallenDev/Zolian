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

    public static string GearEnhancementToString(Item.GearEnhancement e)
    {
        return e switch
        {
            Item.GearEnhancement.None => "None",
            Item.GearEnhancement.One => "One",
            Item.GearEnhancement.Two => "Two",
            Item.GearEnhancement.Three => "Three",
            Item.GearEnhancement.Four => "Four",
            Item.GearEnhancement.Five => "Five",
            Item.GearEnhancement.Six => "Six",
            _ => "None"
        };
    }

    public static string ItemMaterialToString(Item.ItemMaterial e)
    {
        return e switch
        {
            Item.ItemMaterial.None => "None",
            Item.ItemMaterial.Copper => "Copper",
            Item.ItemMaterial.Iron => "Iron",
            Item.ItemMaterial.Steel => "Steel",
            Item.ItemMaterial.Forged => "Forged",
            Item.ItemMaterial.Elven => "Elven",
            Item.ItemMaterial.Dwarven => "Dwarven",
            Item.ItemMaterial.Mythril => "Mythril",
            Item.ItemMaterial.Hybrasyl => "Hybrasyl",
            Item.ItemMaterial.Ebony => "Ebony",
            Item.ItemMaterial.Chaos => "Chaos",
            Item.ItemMaterial.MoonStone => "MoonStone",
            Item.ItemMaterial.SunStone => "SunStone",
            Item.ItemMaterial.Runic => "Runic",
            _ => "None"
        };
    }
}