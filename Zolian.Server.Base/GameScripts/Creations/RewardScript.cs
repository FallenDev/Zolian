using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.GameScripts.Creations;

public abstract class RewardScript : IScriptBase
{
    public abstract void GenerateRewards(Monster monster, Aisling player);

    protected static IEnumerable<string> GenerateRingDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => new List<string>(new[] { "Ribbon Band", "Talos Ring" }),
            >= 6 and <= 11 => new List<string>(new[] { "Royal Silver Ring", "Ruby Ring", "Trinity Band", "Emerald Ring" }),
            >= 12 and <= 20 => new List<string>(new[] { "Amethyst Band", "Jade Ring", "Royal Gold Ring" }),
            >= 21 and <= 32 => new List<string>(new[] { "Sapphire Ring", "Golden Tri-Band" }),
            >= 33 and <= 43 => null,
            >= 44 and <= 54 => null,
            >= 55 and <= 65 => new List<string>(new[] { "Sigma Band", "Garnet Ring" }),
            >= 66 and <= 76 => new List<string>(new[] { "Golden Ribbon Band", "Memory Dulling Ring" }),
            >= 77 and <= 87 => new List<string>(new[] { "Large Emerald Ring", "Large Ruby Ring" }),
            >= 88 and <= 99 => new List<string>(new[] { "Black Pearl Ring" }),
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateBeltDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => new List<string>(new[] { "Fire Belt", "Wind Belt", "Earth Belt", "Sea Belt", "Dark Belt", "Light Belt" }),
            >= 6 and <= 11 => new List<string>(new[] { "Fire Leather Belt", "Wind Leather Belt", "Earth Leather Belt", "Sea Leather Belt" }),
            >= 12 and <= 20 => new List<string>(new[] { "Dark Leather Belt", "Light Leather Belt", "Leather Belt" }),
            >= 21 and <= 32 => new List<string>(new[] { "Fire Mythril Belt", "Wind Mythril Belt", "Earth Mythril Belt", "Sea Mythril Belt" }),
            >= 33 and <= 43 => new List<string>(new[] { "Dark Mythril Belt", "Light Mythril Belt", "Mythril Belt" }),
            >= 44 and <= 54 => new List<string>(new[] { "Fire Hy-Brasyl Belt", "Wind Hy-Brasyl Belt", "Earth Hy-Brasyl Belt", "Sea Hy-Brasyl Belt" }),
            >= 55 and <= 65 => new List<string>(new[] { "Dark Hy-Brasyl Belt", "Light Hy-Brasyl Belt", "Hy-Brasyl Belt" }),
            >= 66 and <= 76 => new List<string>(new[] { "Jeweled Fire Belt", "Jeweled Wind Belt", "Jeweled Earth Belt", "Jeweled Sea Belt" }),
            >= 77 and <= 87 => new List<string>(new[] { "Jeweled Dark Belt", "Jeweled Light Belt" }),
            >= 88 and <= 95 => new List<string>(new[] { "Fire Braided Mythril Belt", "Wind Braided Mythril Belt", "Earth Braided Mythril Belt", "Water Braided Mythril Belt", "Dark Braided Mythril Belt", "Light Braided Mythril Belt" }),
            >= 96 and <= 99 => new List<string>(new[] { "Fire Braided Hy-Brasyl Belt", "Wind Braided Hy-Brasyl Belt", "Earth Braided Hy-Brasyl Belt", "Water Braided Hy-Brasyl Belt", "Dark Braided Hy-Brasyl Belt", "Light Braided Hy-Brasyl Belt" }),
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateBootDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => new List<string>(new[] { "Boots" }),
            >= 6 and <= 11 => new List<string>(new[] { "Grey Boots" }),
            >= 12 and <= 20 => new List<string>(new[] { "Cured Boots" }),
            >= 21 and <= 32 => new List<string>(new[] { "Plague Boots" }),
            >= 33 and <= 43 => new List<string>(new[] { "Shagreen Boots" }),
            >= 44 and <= 54 => new List<string>(new[] { "Silk Boots", "Grim Boots", "Pure Boots" }),
            >= 55 and <= 65 => new List<string>(new[] { "Dust Boots", "Star Boots", "Grace Boots" }),
            >= 66 and <= 76 => new List<string>(new[] { "Saffian Boots" }),
            >= 77 and <= 87 => new List<string>(new[] { "Magma Boots" }),
            >= 88 and <= 99 => new List<string>(new[] { "Enchanted Boots" }),
            >= 100 and <= 109 => new List<string>(new[] { "Enchanted High Boots" }),
            >= 110 and <= 119 => new List<string>(new[] { "Enchanted Slippers" }),
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateEarringDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => new List<string>(new[] { "Beryl Earrings", "Silver Earrings" }),
            >= 6 and <= 11 => new List<string>(new[] { "Coral Earrings" }),
            >= 12 and <= 20 => new List<string>(new[] { "Gold Earrings", "Ruby Earrings", "Pearl Earrings" }),
            >= 21 and <= 32 => new List<string>(new[] { "Skull Clips", "Mythril Earrings" }),
            >= 33 and <= 43 => new List<string>(new[] { "Delicate Gold Earrings" }),
            >= 44 and <= 54 => new List<string>(new[] { "Salve Earrings", "Glaive Earrings" }),
            >= 55 and <= 65 => new List<string>(new[] { "Stalwart Earrings" }),
            >= 66 and <= 76 => new List<string>(new[] { "Duality Earrings" }),
            >= 77 and <= 87 => null,
            >= 88 and <= 99 => new List<string>(new[] { "Feathered Might Clips", "Feathered Mana Clips", "Feathered Insight Clips", "Feathered Stone Clips", "Feathered Enchanted Clips" }),
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => new List<string>(new[] { "Phoenix Earrings" }),
            >= 120 and <= 149 => new List<string>(new[] { "Garnet Studded Earrings", "Sapphire Studded Earrings", "Ruby Studded Earrings", "Emerald Studded Earrings" }),
            >= 150 and <= 179 => new List<string>(new[] { "Ruby Satchel Earrings", "Sapphire Satchel Earrings", "Emerald Satchel Earrings", "Garnet Satchel Earrings" }),
            >= 180 and <= 200 => new List<string>(new[] { "Emblazoned Sapphire Earrings", "Emblazoned Emerald Earrings", "Emblazoned Diamond Earrings", "Emblazoned Garnet Earrings" }),
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateGreaveDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => null,
            >= 6 and <= 11 => new List<string>(new[] { "Leather Greaves" }),
            >= 12 and <= 20 => new List<string>(new[] { "Iron Greaves" }),
            >= 21 and <= 32 => new List<string>(new[] { "Spiked Leather Greaves" }),
            >= 33 and <= 43 => new List<string>(new[] { "Mythril Greaves" }),
            >= 44 and <= 54 => new List<string>(new[] { "Hy-Brasyl Greaves" }),
            >= 55 and <= 65 => null,
            >= 66 and <= 76 => new List<string>(new[] { "Steel Greaves" }),
            >= 77 and <= 87 => new List<string>(new[] { "Light Reinforced Greaves", "Heavy Reinforced Greaves" }),
            >= 88 and <= 99 => new List<string>(new[] { "Light Wing Greaves", "Heavy Wing Greaves" }),
            >= 100 and <= 109 => new List<string>(new[] { "Light Wing Greaves", "Heavy Wing Greaves" }),
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => new List<string>(new [] { "Ebony Shinguards", "Mystic Shinguards" }),
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateHandDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => null,
            >= 6 and <= 11 => new List<string>(new[] { "Leather Gauntlet" }),
            >= 12 and <= 20 => new List<string>(new[] { "Leather Gauntlet" }),
            >= 21 and <= 32 => new List<string>(new[] { "Iron Gauntlet" }),
            >= 33 and <= 43 => new List<string>(new[] { "Padded Gauntlet", "Straw Glove" }),
            >= 44 and <= 54 => new List<string>(new[] { "Padded Gauntlet" }),
            >= 55 and <= 65 => null,
            >= 66 and <= 76 => new List<string>(new[] { "Leopo Glove", "Ringer Glove" }),
            >= 77 and <= 87 => new List<string>(new[] { "Leopo Glove", "Magic Glove" }),
            >= 88 and <= 99 => null,
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateNecklaceDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => null,
            >= 6 and <= 9 => new List<string>(new[] { "Fire Necklace", "Wind Necklace", "Earth Necklace", "Sea Necklace", "Void Necklace", "Holy Necklace", "Pearl Necklace" }),
            >= 10 and <= 20 => new List<string>(new[] { "Fire Gold Jade Necklace", "Wind Gold Jade Necklace", "Earth Gold Jade Necklace", "Sea Gold Jade Necklace", "Void Gold Jade Necklace", "Holy Gold Jade Necklace" }),
            >= 21 and <= 32 => new List<string>(new[] { "Fire Pearl Necklace", "Wind Pearl Necklace", "Earth Pearl Necklace", "Sea Pearl Necklace", "Void Pearl Necklace", "Holy Pearl Necklace" }),
            >= 33 and <= 43 => new List<string>(new[] { "Void Amber Necklace", "Holy Amber Necklace", "Fire Amber Necklace", "Water Amber Necklace", "Wind Amber Necklace", "Earth Amber Necklace" }),
            >= 44 and <= 54 => null,
            >= 55 and <= 65 => null,
            >= 66 and <= 76 => null,
            >= 77 and <= 87 => null,
            >= 88 and <= 99 => new List<string>(new[] { "Fire Kanna Necklace", "Wind Kanna Necklace", "Earth Kanna Necklace", "Water Kanna Necklace", "Void Kanna Necklace", "Holy Kanna Necklace" }),
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateOffHandDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => null,
            >= 6 and <= 11 => null,
            >= 12 and <= 20 => null,
            >= 21 and <= 32 => null,
            >= 33 and <= 43 => null,
            >= 44 and <= 54 => null,
            >= 55 and <= 65 => null,
            >= 66 and <= 76 => null,
            >= 77 and <= 87 => null,
            >= 88 and <= 99 => null,
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateShieldDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => new List<string>(new[] { "Wooden Shield" }),
            >= 6 and <= 11 => new List<string>(new[] { "Floga Wooden Shield", "Thalassa Wooden Shield", "Zephyr Wooden Shield", "Laspi Wooden Shield", "Agios Wooden Shield", "Skia Wooden Shield", "Leather Shield" }),
            >= 12 and <= 25 => new List<string>(new[] { "Floga Leather Shield", "Thalassa Leather Shield", "Zephyr Leather Shield", "Laspi Leather Shield", "Agios Leather Shield", "Skia Leather Shield", "Bronze Shield" }),
            >= 26 and <= 38 => new List<string>(new[] { "Floga Bronze Shield", "Thalassa Bronze Shield", "Zephyr Bronze Shield", "Laspi Bronze Shield", "Agios Bronze Shield", "Skia Bronze Shield", "Etched Shield" }),
            >= 39 and <= 43 => new List<string>(new[] { "Floga Etched Shield", "Thalassa Etched Shield", "Zephyr Etched Shield", "Laspi Etched Shield", "Agios Etched Shield", "Skia Etched Shield" }),
            >= 44 and <= 54 => new List<string>(new[] { "Mythril Shield" }),
            >= 55 and <= 65 => new List<string>(new[] { "Floga Mythril Shield", "Thalassa Mythril Shield", "Zephyr Mythril Shield", "Laspi Mythril Shield", "Agios Mythril Shield", "Skia Mythril Shield", "Hy-Brasyl Shield" }),
            >= 66 and <= 76 => new List<string>(new[] { "Hy-Brasyl Shield" }),
            >= 77 and <= 87 => new List<string>(new[] { "Floga Hy-Brasyl Shield", "Thalassa Hy-Brasyl Shield", "Zephyr Hy-Brasyl Shield", "Laspi Hy-Brasyl Shield", "Agios Hy-Brasyl Shield", "Skia Hy-Brasyl Shield" }),
            >= 88 and <= 99 => new List<string>(new[] { "Talos Shield" }),
            >= 100 and <= 109 => new List<string>(new[] { "Floga Talos Shield", "Thalassa Talos Shield", "Zephyr Talos Shield", "Laspi Talos Shield", "Agios Talos Shield", "Skia Talos Shield" }),
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => new List<string>(new[] { "Talgonite Shield", "Floga Talgonite Shield", "Thalassa Talgonite Shield", "Zephyr Talgonite Shield", "Laspi Talgonite Shield", "Agios Talgonite Shield", "Skia Talgonite Shield" }),
            >= 150 and <= 179 => new List<string>(new[] { "Blood Shield", "Floga Blood Shield", "Thalassa Blood Shield", "Zephyr Blood Shield", "Laspi Blood Shield", "Agios Blood Shield", "Skia Blood Shield" }),
            >= 180 and <= 200 => null,
            _ => null
        };
    }

    protected static IEnumerable<string> GenerateWristDropsBasedOnLevel(Monster monster)
    {
        if (monster == null) return null;
        return monster.Level switch
        {
            >= 1 and <= 5 => null,
            >= 6 and <= 15 => new List<string>(new[] { "Leather Bracer" }),
            >= 16 and <= 20 => new List<string>(new[] { "Leather Bracer" }),
            >= 21 and <= 32 => new List<string>(new[] { "Iron Bracer" }),
            >= 33 and <= 43 => new List<string>(new[] { "Chain Bracer" }),
            >= 44 and <= 54 => new List<string>(new[] { "Chain Bracer" }),
            >= 55 and <= 65 => new List<string>(new[] { "Signet Bracer" }),
            >= 66 and <= 76 => null,
            >= 77 and <= 87 => new List<string>(new[] { "Spiked Bracer" }),
            >= 88 and <= 99 => new List<string>(new[] { "Spiked Bracer" }),
            >= 100 and <= 109 => null,
            >= 110 and <= 119 => null,
            >= 120 and <= 149 => null,
            >= 150 and <= 179 => null,
            >= 180 and <= 200 => null,
            _ => null
        };
    }
}