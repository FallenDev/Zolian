using Darkages.Common;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

public static class ItemQualityVariance
{
    public static void ItemDurability(Item item, Item.Quality quality)
    {
        var temp = item.Template.MaxDurability;
        switch (quality)
        {
            case Item.Quality.Damaged:
                item.MaxDurability = (uint)(temp / 1.4);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Common:
                item.MaxDurability = temp / 1;
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Uncommon:
                item.MaxDurability = (uint)(temp / 0.9);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Rare:
                item.MaxDurability = (uint)(temp / 0.8);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Epic:
                item.MaxDurability = (uint)(temp / 0.7);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Legendary:
                item.MaxDurability = (uint)(temp / 0.6);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Forsaken:
                item.MaxDurability = (uint)(temp / 0.5);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Mythic:
                item.MaxDurability = (uint)(temp / 0.3);
                item.Durability = item.MaxDurability;
                break;
        }
    }

    public static void SetMaxItemDurability(Item item, Item.Quality quality)
    {
        var temp = item.Template.MaxDurability;
        switch (quality)
        {
            case Item.Quality.Damaged:
                item.MaxDurability = (uint)(temp / 1.4);
                break;
            case Item.Quality.Common:
                item.MaxDurability = temp / 1;
                break;
            case Item.Quality.Uncommon:
                item.MaxDurability = (uint)(temp / 0.9);
                break;
            case Item.Quality.Rare:
                item.MaxDurability = (uint)(temp / 0.8);
                break;
            case Item.Quality.Epic:
                item.MaxDurability = (uint)(temp / 0.7);
                break;
            case Item.Quality.Legendary:
                item.MaxDurability = (uint)(temp / 0.6);
                break;
            case Item.Quality.Forsaken:
                item.MaxDurability = (uint)(temp / 0.5);
                break;
            case Item.Quality.Mythic:
                item.MaxDurability = (uint)(temp / 0.2);
                break;
        }
    }

    public static Item.Quality DetermineQuality()
    {
        var qualityGen = Generator.RandomNumPercentGen();

        return qualityGen switch
        {
            >= 0 and <= .20 => Item.Quality.Damaged,
            > .20 and <= .80 => Item.Quality.Common,
            > .80 and <= .90 => Item.Quality.Uncommon,
            > .90 and <= .97 => Item.Quality.Rare,
            > .97 and <= .99 => Item.Quality.Epic,
            > .99 and <= .9975 => Item.Quality.Legendary,
            > .9975 and <= .9998 => Item.Quality.Forsaken,
            > .9998 and <= 1 => Item.Quality.Mythic,
            _ => Item.Quality.Damaged
        };
    }

    public static Item.Quality DetermineHighQuality()
    {
        var qualityGen = Generator.RandomNumPercentGen();

        return qualityGen switch
        {
            >= 0 and <= .20 => Item.Quality.Rare,
            > .77 and <= .89 => Item.Quality.Epic,
            > .89 and <= .99 => Item.Quality.Legendary,
            > .99 and <= .991 => Item.Quality.Forsaken,
            > .991 and <= 1 => Item.Quality.Mythic,
            _ => Item.Quality.Rare
        };
    }

    public static Item.Variance DetermineVariance() => Generator.RandomEnumValue<Item.Variance>();

    public static Item.WeaponVariance DetermineWeaponVariance()
    {
        var weaponVariance = Generator.RandomNumPercentGen();

        return weaponVariance switch
        {
            >= 0 and <= .64 => Item.WeaponVariance.None,
            >= .65 and <= .67 => Item.WeaponVariance.Aegis,
            >= .68 and <= .70 => Item.WeaponVariance.Bleeding,
            >= .71 and <= .73 => Item.WeaponVariance.Rending,
            >= .74 and <= .76 => Item.WeaponVariance.Reaping,
            >= .77 and <= .79 => Item.WeaponVariance.Vampirism,
            >= .80 and <= .82 => Item.WeaponVariance.Haste,
            >= .83 and <= .85 => Item.WeaponVariance.Gust,
            >= .86 and <= .88 => Item.WeaponVariance.Quake,
            >= .89 and <= .91 => Item.WeaponVariance.Rain,
            >= .92 and <= .94 => Item.WeaponVariance.Flame,
            >= .95 and <= .97 => Item.WeaponVariance.Dusk,
            >= .98 and <= 1 => Item.WeaponVariance.Dawn,
            _ => Item.WeaponVariance.None
        };
    }
}