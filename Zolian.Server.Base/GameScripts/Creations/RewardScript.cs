using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.GameScripts.Creations;

public abstract class RewardScript : IScriptBase
{
    public abstract void GenerateRewards(Monster monster, Aisling player);

    protected static readonly Dictionary<(int, int), List<string>> RingDrops = new()
    {
        [(1, 5)] = new List<string> { "Ribbon Band", "Talos Ring" },
        [(6, 11)] = new List<string> { "Royal Silver Ring", "Ruby Ring", "Trinity Band", "Emerald Ring" },
        [(12, 20)] = new List<string> { "Amethyst Band", "Jade Ring", "Royal Gold Ring" },
        [(21, 32)] = new List<string> { "Sapphire Ring", "Golden Tri-Band" },
        [(33, 43)] = null,
        [(44, 54)] = null,
        [(55, 65)] = new List<string> { "Sigma Band", "Garnet Ring" },
        [(66, 76)] = new List<string> { "Golden Ribbon Band", "Memory Dulling Ring" },
        [(77, 87)] = new List<string> { "Large Emerald Ring", "Large Ruby Ring" },
        [(88, 99)] = new List<string> { "Black Pearl Ring" },
        [(100, 109)] = null,
        [(110, 119)] = null,
        [(120, 149)] = null,
        [(150, 179)] = null,
        [(180, 200)] = null,
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> BeltDrops = new()
    {
        [(1, 5)] = new List<string> { "Fire Belt", "Wind Belt", "Earth Belt", "Sea Belt", "Dark Belt", "Light Belt" },
        [(6, 11)] = new List<string> { "Fire Leather Belt", "Wind Leather Belt", "Earth Leather Belt", "Sea Leather Belt" },
        [(12, 20)] = new List<string> { "Dark Leather Belt", "Light Leather Belt", "Leather Belt" },
        [(21, 32)] = new List<string> { "Fire Mythril Belt", "Wind Mythril Belt", "Earth Mythril Belt", "Sea Mythril Belt" },
        [(33, 43)] = new List<string> { "Dark Mythril Belt", "Light Mythril Belt", "Mythril Belt" },
        [(44, 54)] = new List<string> { "Fire Hy-Brasyl Belt", "Wind Hy-Brasyl Belt", "Earth Hy-Brasyl Belt", "Sea Hy-Brasyl Belt" },
        [(55, 65)] = new List<string> { "Dark Hy-Brasyl Belt", "Light Hy-Brasyl Belt", "Hy-Brasyl Belt" },
        [(66, 76)] = new List<string> { "Jeweled Fire Belt", "Jeweled Wind Belt", "Jeweled Earth Belt", "Jeweled Sea Belt" },
        [(77, 87)] = new List<string> { "Jeweled Dark Belt", "Jeweled Light Belt" },
        [(88, 95)] = new List<string> { "Fire Braided Mythril Belt", "Wind Braided Mythril Belt", "Earth Braided Mythril Belt", "Water Braided Mythril Belt", "Dark Braided Mythril Belt", "Light Braided Mythril Belt" },
        [(96, 99)] = new List<string> { "Fire Braided Hy-Brasyl Belt", "Wind Braided Hy-Brasyl Belt", "Earth Braided Hy-Brasyl Belt", "Water Braided Hy-Brasyl Belt", "Dark Braided Hy-Brasyl Belt", "Light Braided Hy-Brasyl Belt" },
        [(100, 109)] = null,
        [(110, 119)] = new List<string> { "Void Fringe Belt", "Holy Fringe Belt", "Fire Fringe Belt", "Wind Fringe Belt", "Earth Fringe Belt", "Sea Fringe Belt" },
        [(120, 149)] = new List<string> { "Void Wade Belt", "Holy Wade Belt", "Fire Wade Belt", "Wind Wade Belt", "Earth Wade Belt", "Water Wade Belt" },
        [(150, 179)] = new List<string> { "Void Clasp Belt", "Holy Clasp Belt", "Fire Clasp Belt", "Wind Clasp Belt", "Earth Clasp Belt", "Sea Clasp Belt" },
        [(180, 200)] = new List<string> { "Flame Intricate Girdle", "Wind Intricate Girdle", "Earth Intricate Girdle", "Sea Intricate Girdle" },
        [(201, 225)] = new List<string> { "Flame Fortified Girdle", "Wind Fortified Girdle", "Earth Fortified Girdle", "Sea Fortified Girdle" },
        [(226, 249)] = null,
        [(250, 299)] = new List<string> { "Cursed Belt", "Polished Hybrasyl Girdle" },
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> BootDrops = new()
    {
        [(1, 5)] = new List<string> { "Boots" },
        [(6, 11)] = new List<string> { "Grey Boots" },
        [(12, 20)] = new List<string> { "Cured Boots" },
        [(21, 32)] = new List<string> { "Plague Boots" },
        [(33, 43)] = new List<string> { "Shagreen Boots" },
        [(44, 54)] = new List<string> { "Silk Boots", "Grim Boots", "Pure Boots" },
        [(55, 65)] = new List<string> { "Dust Boots", "Star Boots", "Grace Boots" },
        [(66, 76)] = new List<string> { "Saffian Boots" },
        [(77, 87)] = new List<string> { "Magma Boots" },
        [(88, 99)] = new List<string> { "Enchanted Boots" },
        [(100, 109)] = new List<string> { "Enchanted High Boots" },
        [(110, 119)] = new List<string> { "Enchanted Slippers" },
        [(120, 149)] = new List<string> { "Arctic Walkers" },
        [(150, 179)] = new List<string> { "Mulberry Treads", "Swamp Wadders" },
        [(180, 200)] = new List<string> { "Royal Treads" },
        [(201, 225)] = new List<string> { "Mythic Slippers", "Hybrasyl Boots" },
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> EarringDrops = new()
    {
        [(1, 5)] = new List<string> { "Beryl Earrings", "Silver Earrings" },
        [(6, 11)] = new List<string> { "Coral Earrings" },
        [(12, 20)] = new List<string> { "Gold Earrings", "Ruby Earrings", "Pearl Earrings" },
        [(21, 32)] = new List<string> { "Skull Clips", "Mythril Earrings" },
        [(33, 43)] = new List<string> { "Delicate Gold Earrings" },
        [(44, 54)] = new List<string> { "Salve Earrings", "Glaive Earrings" },
        [(55, 65)] = new List<string> { "Stalwart Earrings" },
        [(66, 76)] = new List<string> { "Duality Earrings" },
        [(77, 87)] = null,
        [(88, 99)] = new List<string> { "Feathered Might Clips", "Feathered Mana Clips", "Feathered Insight Clips", "Feathered Stone Clips", "Feathered Enchanted Clips" },
        [(100, 109)] = null,
        [(110, 119)] = new List<string> { "Phoenix Earrings" },
        [(120, 149)] = new List<string> { "Garnet Studded Earrings", "Sapphire Studded Earrings", "Ruby Studded Earrings", "Emerald Studded Earrings" },
        [(150, 179)] = new List<string> { "Ruby Satchel Earrings", "Sapphire Satchel Earrings", "Emerald Satchel Earrings", "Garnet Satchel Earrings" },
        [(180, 200)] = new List<string> { "Emblazoned Sapphire Earrings", "Emblazoned Emerald Earrings", "Emblazoned Diamond Earrings", "Emblazoned Garnet Earrings" },
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> GreaveDrops = new()
    {
        [(1, 5)] = null,
        [(6, 11)] = new List<string> { "Leather Greaves" },
        [(12, 20)] = new List<string> { "Iron Greaves" },
        [(21, 32)] = new List<string> { "Spiked Leather Greaves" },
        [(33, 43)] = new List<string> { "Mythril Greaves" },
        [(44, 54)] = new List<string> { "Hy-Brasyl Greaves" },
        [(55, 65)] = null,
        [(66, 76)] = new List<string> { "Steel Greaves" },
        [(77, 87)] = new List<string> { "Light Reinforced Greaves", "Heavy Reinforced Greaves" },
        [(88, 99)] = new List<string> { "Light Wing Greaves", "Heavy Wing Greaves" },
        [(100, 109)] = new List<string> { "Light Wing Greaves", "Heavy Wing Greaves" },
        [(110, 119)] = null,
        [(120, 149)] = new List<string> { "Ebony Shinguards", "Mystic Shinguards" },
        [(150, 179)] = new List<string> { "Gravel Poleyns", "Gust Poleyns", "Flame Poleyns", "Water Spout Poleyns" },
        [(180, 200)] = new List<string> { "Enlightened Poleyns" },
        [(201, 225)] = new List<string> { "Lava Poleyns" },
        [(226, 249)] = new List<string> { "Forest Poleyns" },
        [(250, 299)] = new List<string> { "Mana Guards", "Blade Poleyns" },
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> HandDrops = new()
    {
        [(1, 5)] = null,
        [(6, 11)] = new List<string> { "Leather Gauntlet" },
        [(12, 20)] = new List<string> { "Leather Gauntlet" },
        [(21, 32)] = new List<string> { "Iron Gauntlet" },
        [(33, 43)] = new List<string> { "Padded Gauntlet", "Straw Glove" },
        [(44, 54)] = new List<string> { "Padded Gauntlet" },
        [(55, 65)] = null,
        [(66, 76)] = new List<string> { "Leopo Glove", "Ringer Glove" },
        [(77, 87)] = new List<string> { "Leopo Glove", "Magic Glove" },
        [(88, 99)] = new List<string> { "Mythril Gauntlet" },
        [(100, 109)] = new List<string> { "Hybrasyl Gauntlet" },
        [(110, 119)] = null,
        [(120, 149)] = new List<string> { "Scurvy Gauntlet", "Amorphous Gauntlet" },
        [(150, 179)] = new List<string> { "Kandor Glove" },
        [(180, 200)] = new List<string> { "Bran Vambrace" },
        [(201, 225)] = new List<string> { "Aquatis Vambrace" },
        [(226, 249)] = new List<string> { "Ruby Hybrasyl Gauntlet" },
        [(250, 299)] = new List<string> { "Juoinior Gauntlet" },
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> NecklaceDrops = new()
    {
        [(1, 5)] = null,
        [(6, 9)] = new List<string> { "Fire Necklace", "Wind Necklace", "Earth Necklace", "Sea Necklace", "Void Necklace", "Holy Necklace", "Pearl Necklace" },
        [(10, 20)] = new List<string> { "Fire Gold Jade Necklace", "Wind Gold Jade Necklace", "Earth Gold Jade Necklace", "Sea Gold Jade Necklace", "Void Gold Jade Necklace", "Holy Gold Jade Necklace" },
        [(21, 32)] = new List<string> { "Fire Pearl Necklace", "Wind Pearl Necklace", "Earth Pearl Necklace", "Sea Pearl Necklace", "Void Pearl Necklace", "Holy Pearl Necklace" },
        [(33, 43)] = new List<string> { "Void Amber Necklace", "Holy Amber Necklace", "Fire Amber Necklace", "Water Amber Necklace", "Wind Amber Necklace", "Earth Amber Necklace" },
        [(44, 54)] = null,
        [(55, 65)] = null,
        [(66, 76)] = null,
        [(77, 87)] = new List<string> { "Fire Kanna Necklace", "Wind Kanna Necklace", "Earth Kanna Necklace", "Water Kanna Necklace", "Void Kanna Necklace", "Holy Kanna Necklace" },
        [(88, 99)] = null,
        [(100, 109)] = new List<string> { "Fire Cascading Necklace", "Wind Cascading Necklace", "Earth Cascading Necklace", "Water Cascading Necklace", "Void Cascading Necklace", "Holy Cascading Necklace" },
        [(110, 119)] = null,
        [(120, 149)] = new List<string> { "Fire Encrusted Necklace", "Wind Encrusted Necklace", "Earth Encrusted Necklace", "Water Encrusted Necklace", "Void Encrusted Necklace", "Holy Encrusted Necklace" },
        [(150, 179)] = new List<string> { "Fire Diamat Necklace", "Wind Diamat Necklace", "Earth Diamat Necklace", "Water Diamat Necklace", "Void Diamat Necklace", "Holy Diamat Necklace" },
        [(180, 200)] = new List<string> { "Children's Crux", "Fervent Mala" },
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> OffHandDrops = new()
    {
        [(1, 5)] = null,
        [(6, 11)] = null,
        [(12, 20)] = null,
        [(21, 32)] = null,
        [(33, 43)] = null,
        [(44, 54)] = null,
        [(55, 65)] = null,
        [(66, 76)] = null,
        [(77, 87)] = null,
        [(88, 99)] = null,
        [(100, 109)] = null,
        [(110, 119)] = null,
        [(120, 149)] = null,
        [(150, 179)] = null,
        [(180, 200)] = null,
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> ShieldDrops = new()
    {
        [(1, 5)] = new List<string> { "Wooden Shield" },
        [(6, 11)] = new List<string> { "Floga Wooden Shield", "Thalassa Wooden Shield", "Zephyr Wooden Shield", "Laspi Wooden Shield", "Agios Wooden Shield", "Skia Wooden Shield", "Leather Shield" },
        [(12, 25)] = new List<string> { "Floga Leather Shield", "Thalassa Leather Shield", "Zephyr Leather Shield", "Laspi Leather Shield", "Agios Leather Shield", "Skia Leather Shield", "Bronze Shield" },
        [(26, 38)] = new List<string> { "Floga Bronze Shield", "Thalassa Bronze Shield", "Zephyr Bronze Shield", "Laspi Bronze Shield", "Agios Bronze Shield", "Skia Bronze Shield", "Etched Shield" },
        [(39, 43)] = new List<string> { "Floga Etched Shield", "Thalassa Etched Shield", "Zephyr Etched Shield", "Laspi Etched Shield", "Agios Etched Shield", "Skia Etched Shield" },
        [(44, 54)] = new List<string> { "Mythril Shield" },
        [(55, 65)] = new List<string> { "Floga Mythril Shield", "Thalassa Mythril Shield", "Zephyr Mythril Shield", "Laspi Mythril Shield", "Agios Mythril Shield", "Skia Mythril Shield", "Hy-Brasyl Shield" },
        [(66, 76)] = new List<string> { "Hy-Brasyl Shield" },
        [(77, 87)] = new List<string> { "Floga Hy-Brasyl Shield", "Thalassa Hy-Brasyl Shield", "Zephyr Hy-Brasyl Shield", "Laspi Hy-Brasyl Shield", "Agios Hy-Brasyl Shield", "Skia Hy-Brasyl Shield" },
        [(88, 99)] = new List<string> { "Talos Shield" },
        [(100, 109)] = new List<string> { "Floga Talos Shield", "Thalassa Talos Shield", "Zephyr Talos Shield", "Laspi Talos Shield", "Agios Talos Shield", "Skia Talos Shield" },
        [(110, 119)] = null,
        [(120, 149)] = new List<string> { "Talgonite Shield", "Floga Talgonite Shield", "Thalassa Talgonite Shield", "Zephyr Talgonite Shield", "Laspi Talgonite Shield", "Agios Talgonite Shield", "Skia Talgonite Shield" },
        [(150, 179)] = new List<string> { "Blood Shield", "Floga Blood Shield", "Thalassa Blood Shield", "Zephyr Blood Shield", "Laspi Blood Shield", "Agios Blood Shield", "Skia Blood Shield" },
        [(180, 200)] = null,
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> WristDrops = new()
    {
        [(1, 5)] = null,
        [(6, 15)] = new List<string> { "Leather Bracer" },
        [(16, 20)] = new List<string> { "Leather Bracer" },
        [(21, 32)] = new List<string> { "Iron Bracer" },
        [(33, 43)] = new List<string> { "Chain Bracer" },
        [(44, 54)] = new List<string> { "Chain Bracer" },
        [(55, 65)] = new List<string> { "Signet Bracer" },
        [(66, 76)] = null,
        [(77, 87)] = new List<string> { "Spiked Bracer" },
        [(88, 99)] = new List<string> { "Spiked Bracer" },
        [(100, 109)] = null,
        [(110, 119)] = null,
        [(120, 149)] = null,
        [(150, 179)] = new List<string> { "Scale Bracer" },
        [(180, 200)] = null,
        [(201, 225)] = null,
        [(226, 249)] = new List<string> { "Juggernaut Glove" },
        [(250, 299)] = new List<string> { "Talon Gauntlet" },
        [(300, 349)] = new List<string> { "Golden Talon Gauntlet" },
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null
    };

    protected static IEnumerable<string> GenerateDropsBasedOnLevel(Monster monster, Dictionary<(int, int), List<string>> dropsDict)
    {
        return monster == null ? null : (from entry in dropsDict where monster.Level >= entry.Key.Item1 && monster.Level <= entry.Key.Item2 select entry.Value).FirstOrDefault();
    }
}