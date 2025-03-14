﻿using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Creations;

public abstract class RewardScript
{
    public abstract void GenerateRewards(Monster monster, Aisling player);
    public abstract void GenerateInanimateRewards(Monster monster, Aisling player);

    protected static readonly Dictionary<(int, int), List<string>> RingDrops = new()
    {
        [(1, 5)] = ["Ribbon Band", "Talos Ring"],
        [(6, 11)] = ["Royal Silver Ring", "Ruby Ring", "Trinity Band", "Emerald Ring"],
        [(12, 20)] = ["Amethyst Band", "Jade Ring", "Royal Gold Ring"],
        [(21, 32)] = ["Sapphire Ring", "Golden Tri-Band"],
        [(33, 43)] = null,
        [(44, 54)] = null,
        [(55, 65)] = ["Sigma Band", "Garnet Ring"],
        [(66, 76)] = ["Golden Ribbon Band", "Memory Dulling Ring"],
        [(77, 87)] = ["Large Emerald Ring", "Large Ruby Ring"],
        [(88, 99)] = ["Black Pearl Ring"],
        [(100, 109)] = null,
        [(110, 119)] = null,
        [(120, 149)] = ["Diamond Ring", "Entangled Loop"],
        [(150, 179)] = ["Royal Sapphire Signet", "Silver Emerald Band"],
        [(180, 200)] = ["Heartstone Blank Pearl Ring", "Dread Band"],
        [(201, 225)] = null,
        [(226, 249)] = ["Blackstone Ring"],
        [(250, 299)] = ["Flackern Band", "Blazed Ring"],
        [(300, 349)] = null,
        [(350, 399)] = ["Folt Demon Band"],
        [(400, 449)] = ["Neart Fire Ring", "Fios Ice Ring"],
        [(450, 500)] = ["Rioga Kings Ring"],
        [(501, 550)] = null,
        [(551, 600)] = null,
        [(601, 650)] = null,
        [(651, 700)] = null,
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected internal static readonly Dictionary<(int, int), List<string>> BeltDrops = new()
    {
        [(1, 5)] = ["Fire Belt", "Wind Belt", "Earth Belt", "Sea Belt", "Dark Belt", "Light Belt"],
        [(6, 11)] = ["Fire Leather Belt", "Wind Leather Belt", "Earth Leather Belt", "Sea Leather Belt"],
        [(12, 20)] = ["Dark Leather Belt", "Light Leather Belt", "Leather Belt"],
        [(21, 32)] = ["Fire Mythril Belt", "Wind Mythril Belt", "Earth Mythril Belt", "Sea Mythril Belt"],
        [(33, 43)] = ["Dark Mythril Belt", "Light Mythril Belt", "Mythril Belt"],
        [(44, 54)] = ["Fire Hy-Brasyl Belt", "Wind Hy-Brasyl Belt", "Earth Hy-Brasyl Belt", "Sea Hy-Brasyl Belt"],
        [(55, 65)] = ["Dark Hy-Brasyl Belt", "Light Hy-Brasyl Belt", "Hy-Brasyl Belt"],
        [(66, 76)] = ["Jeweled Fire Belt", "Jeweled Wind Belt", "Jeweled Earth Belt", "Jeweled Sea Belt"],
        [(77, 87)] = ["Jeweled Dark Belt", "Jeweled Light Belt"],
        [(88, 95)] = ["Fire Braided Mythril Belt", "Wind Braided Mythril Belt", "Earth Braided Mythril Belt", "Water Braided Mythril Belt", "Dark Braided Mythril Belt", "Light Braided Mythril Belt"],
        [(96, 99)] = ["Fire Braided Hy-Brasyl Belt", "Wind Braided Hy-Brasyl Belt", "Earth Braided Hy-Brasyl Belt", "Water Braided Hy-Brasyl Belt", "Dark Braided Hy-Brasyl Belt", "Light Braided Hy-Brasyl Belt"],
        [(100, 109)] = null,
        [(110, 119)] = ["Void Fringe Belt", "Holy Fringe Belt", "Fire Fringe Belt", "Wind Fringe Belt", "Earth Fringe Belt", "Sea Fringe Belt"],
        [(120, 149)] = ["Void Wade Belt", "Holy Wade Belt", "Fire Wade Belt", "Wind Wade Belt", "Earth Wade Belt", "Water Wade Belt"],
        [(150, 179)] = ["Void Clasp Belt", "Holy Clasp Belt", "Fire Clasp Belt", "Wind Clasp Belt", "Earth Clasp Belt", "Sea Clasp Belt"],
        [(180, 200)] = ["Flame Intricate Girdle", "Wind Intricate Girdle", "Earth Intricate Girdle", "Sea Intricate Girdle"],
        [(201, 225)] = ["Flame Fortified Girdle", "Wind Fortified Girdle", "Earth Fortified Girdle", "Sea Fortified Girdle"],
        [(226, 249)] = null,
        [(250, 299)] = ["Cursed Belt", "Polished Hybrasyl Girdle"],
        [(300, 349)] = ["Dark Red Plamit Belt", "Light Red Plamit Belt"],
        [(350, 399)] = null,
        [(400, 449)] = ["Flame Sarnath Cingulum", "Wind Sarnath Cingulum", "Earth Sarnath Cingulum", "Sea Sarnath Cingulum"],
        [(450, 500)] = ["Voided Sarnath Cingulum", "Divine Sarnath Cingulum"],
        [(501, 550)] = ["Flame-Etched Dragon's Cingulum", "Wind-Etched Dragon's Cingulum", "Earth-Etched Dragon's Cingulum", "Sea-Etched Dragon's Cingulum"],
        [(551, 600)] = ["Flame-Etched Dragon's Cingulum", "Wind-Etched Dragon's Cingulum", "Earth-Etched Dragon's Cingulum", "Sea-Etched Dragon's Cingulum", "Void-Etched Dragon's Cingulum", "Holy-Etched Dragon's Cingulum"],
        [(601, 650)] = ["Void-Etched Dragon's Cingulum", "Holy-Etched Dragon's Cingulum"],
        [(651, 700)] = ["Skull Dragon's Void Clasp", "Skull Dragon's Holy Clasp"],
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> BootDrops = new()
    {
        [(1, 5)] = ["Boots"],
        [(6, 11)] = ["Grey Boots"],
        [(12, 20)] = ["Cured Boots"],
        [(21, 32)] = ["Plague Boots"],
        [(33, 43)] = ["Shagreen Boots"],
        [(44, 54)] = ["Silk Boots", "Grim Boots", "Pure Boots"],
        [(55, 65)] = ["Dust Boots", "Star Boots", "Grace Boots"],
        [(66, 76)] = ["Saffian Boots"],
        [(77, 87)] = ["Magma Boots"],
        [(88, 99)] = ["Enchanted Boots"],
        [(100, 109)] = ["Enchanted High Boots"],
        [(110, 119)] = ["Enchanted Slippers"],
        [(120, 149)] = ["Arctic Walkers"],
        [(150, 179)] = ["Mulberry Treads", "Swamp Wadders"],
        [(180, 200)] = ["Royal Treads"],
        [(201, 225)] = ["Mythic Slippers", "Hybrasyl Boots"],
        [(226, 249)] = ["Shinguards"],
        [(250, 299)] = ["Leather Strapped Sabatons"],
        [(300, 349)] = ["Red Plamit Boots"],
        [(350, 399)] = ["Red Plamit Boots", "Silver Dragon Boots", "Golden Dragon Boots"],
        [(400, 449)] = ["Silver Dragon Boots", "Golden Dragon Boots"],
        [(450, 500)] = ["Atlantis Sandals"],
        [(501, 550)] = ["Atlantis Sandals", "Depth Voyagers"],
        [(551, 600)] = ["Obsidian Tipped Voyagers"],
        [(601, 650)] = ["Laced Obsidian Boots"],
        [(651, 700)] = null,
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> EarringDrops = new()
    {
        [(1, 5)] = ["Beryl Earrings", "Silver Earrings"],
        [(6, 11)] = ["Coral Earrings"],
        [(12, 20)] = ["Gold Earrings", "Ruby Earrings", "Pearl Earrings"],
        [(21, 32)] = ["Skull Clips", "Mythril Earrings"],
        [(33, 43)] = ["Delicate Gold Earrings"],
        [(44, 54)] = ["Salve Earrings", "Glaive Earrings"],
        [(55, 65)] = ["Stalwart Earrings"],
        [(66, 76)] = ["Duality Earrings"],
        [(77, 87)] = null,
        [(88, 99)] = ["Feathered Might Clips", "Feathered Mana Clips", "Feathered Insight Clips", "Feathered Stone Clips", "Feathered Enchanted Clips"],
        [(100, 109)] = null,
        [(110, 119)] = ["Phoenix Earrings"],
        [(120, 149)] = ["Garnet Studded Earrings", "Sapphire Studded Earrings", "Ruby Studded Earrings", "Emerald Studded Earrings"],
        [(150, 179)] = ["Ruby Satchel Earrings", "Sapphire Satchel Earrings", "Emerald Satchel Earrings", "Garnet Satchel Earrings"],
        [(180, 200)] = ["Emblazoned Sapphire Earrings", "Emblazoned Emerald Earrings", "Emblazoned Diamond Earrings", "Emblazoned Garnet Earrings"],
        [(201, 225)] = ["Elementus Blaze Earrings", "Elementus Blizzard Earrings", "Elementus Typhoon Earrings", "Elementus Shatter Earrings"],
        [(226, 249)] = ["Elementus Blaze Earrings", "Elementus Blizzard Earrings", "Elementus Typhoon Earrings", "Elementus Shatter Earrings"],
        [(250, 299)] = null,
        [(300, 349)] = ["Ruby Triforce Earrings", "Empowered Ruby Triforce Earrings"],
        [(350, 399)] = ["Gold Triforce Earrings", "Empowered Gold Triforce Earrings"],
        [(400, 449)] = ["Stone Studded Clips"],
        [(450, 500)] = ["Rioga Orb Earrings"],
        [(501, 550)] = ["Cailius Jewel"],
        [(551, 600)] = ["Cailius Jewel", "Dual Cailius Jewels"],
        [(601, 650)] = ["Ruby Imperius Earrings", "Emerald Imperius Earrings"],
        [(651, 700)] = ["Golden Imperius Earrings"],
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> GreaveDrops = new()
    {
        [(1, 5)] = null,
        [(6, 11)] = ["Leather Greaves"],
        [(12, 20)] = ["Iron Greaves"],
        [(21, 32)] = ["Spiked Leather Greaves"],
        [(33, 43)] = ["Mythril Greaves"],
        [(44, 54)] = ["Hy-Brasyl Greaves"],
        [(55, 65)] = null,
        [(66, 76)] = ["Steel Greaves"],
        [(77, 87)] = ["Light Reinforced Greaves", "Heavy Reinforced Greaves"],
        [(88, 99)] = ["Light Wing Greaves", "Heavy Wing Greaves"],
        [(100, 109)] = ["Light Wing Greaves", "Heavy Wing Greaves"],
        [(110, 119)] = null,
        [(120, 149)] = ["Ebony Shinguards", "Mystic Shinguards"],
        [(150, 179)] = ["Gravel Poleyns", "Gust Poleyns", "Flame Poleyns", "Water Spout Poleyns"],
        [(180, 200)] = ["Enlightened Poleyns"],
        [(201, 225)] = ["Lava Poleyns"],
        [(226, 249)] = ["Forest Poleyns"],
        [(250, 299)] = ["Mana Guards", "Blade Poleyns"],
        [(300, 349)] = ["Red Plamit Greaves", "Fios Poleyns", "Dith Poleyns", "Gnimh Poleyns", "Neart Poleyns"],
        [(350, 399)] = ["Fios Poleyns", "Dith Poleyns", "Gnimh Poleyns", "Neart Poleyns"],
        [(400, 449)] = ["Sacra Poleyns"],
        [(450, 500)] = ["Gaeth Poleyns"],
        [(501, 550)] = ["Ruined Shinguards"],
        [(551, 600)] = null,
        [(601, 650)] = ["Ice Dragon's Infused Guards"],
        [(651, 700)] = ["Skull Dragon's Guards"],
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> HandDrops = new()
    {
        [(1, 5)] = null,
        [(6, 11)] = ["Leather Gauntlet"],
        [(12, 20)] = ["Leather Gauntlet", "Iron Gauntlet"],
        [(21, 32)] = ["Iron Gauntlet"],
        [(33, 43)] = ["Padded Gauntlet", "Straw Glove"],
        [(44, 54)] = ["Padded Gauntlet"],
        [(55, 65)] = ["Ringer Glove"],
        [(66, 76)] = ["Leopo Glove", "Ringer Glove"],
        [(77, 87)] = ["Leopo Glove", "Magic Glove"],
        [(88, 99)] = ["Mythril Gauntlet"],
        [(100, 109)] = ["Hybrasyl Gauntlet"],
        [(110, 119)] = null,
        [(120, 149)] = ["Scurvy Gauntlet", "Amorphous Gauntlet"],
        [(150, 179)] = ["Kandor Glove"],
        [(180, 200)] = ["Bran Vambrace"],
        [(201, 225)] = ["Aquatis Vambrace"],
        [(226, 249)] = ["Ruby Hybrasyl Gauntlet"],
        [(250, 299)] = ["Juoinior Gauntlet"],
        [(300, 349)] = ["Red Plamit Glove", "Encrusted Grasper"],
        [(350, 399)] = ["Encrusted Grasper", "Depth Encrusted Grasper"],
        [(400, 449)] = ["Depth Encrusted Grasper"],
        [(450, 500)] = ["Heavy Depth Encrusted Grasper"],
        [(501, 550)] = ["Flawless Depth Grasper"],
        [(551, 600)] = ["Ruined Arm Guards"],
        [(601, 650)] = ["Knight's Armguard", "Bowman's Guard"],
        [(651, 700)] = ["Knight's Heavy Armguard"],
        [(701, 750)] = ["Void Dragon's Grasper"],
        [(751, 800)] = ["Ice Dragon's Gauntlet"],
        [(801, 850)] = ["Chaos Dragon's Gauntlet"],
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> NecklaceDrops = new()
    {
        [(1, 5)] = ["Fire Necklace", "Wind Necklace", "Earth Necklace", "Sea Necklace", "Void Necklace", "Holy Necklace", "Pearl Necklace"],
        [(6, 9)] = ["Fire Necklace", "Wind Necklace", "Earth Necklace", "Sea Necklace", "Void Necklace", "Holy Necklace", "Pearl Necklace"],
        [(10, 20)] = ["Fire Gold Jade Necklace", "Wind Gold Jade Necklace", "Earth Gold Jade Necklace", "Sea Gold Jade Necklace", "Void Gold Jade Necklace", "Holy Gold Jade Necklace"],
        [(21, 32)] = ["Fire Pearl Necklace", "Wind Pearl Necklace", "Earth Pearl Necklace", "Sea Pearl Necklace", "Void Pearl Necklace", "Holy Pearl Necklace"],
        [(33, 43)] = ["Void Amber Necklace", "Holy Amber Necklace", "Fire Amber Necklace", "Water Amber Necklace", "Wind Amber Necklace", "Earth Amber Necklace"],
        [(44, 54)] = ["Void Amber Necklace", "Holy Amber Necklace", "Fire Amber Necklace", "Water Amber Necklace", "Wind Amber Necklace", "Earth Amber Necklace"],
        [(55, 65)] = null,
        [(66, 76)] = null,
        [(77, 87)] = ["Fire Kanna Necklace", "Wind Kanna Necklace", "Earth Kanna Necklace", "Water Kanna Necklace", "Void Kanna Necklace", "Holy Kanna Necklace"],
        [(88, 99)] = ["Fire Kanna Necklace", "Wind Kanna Necklace", "Earth Kanna Necklace", "Water Kanna Necklace", "Void Kanna Necklace", "Holy Kanna Necklace"],
        [(100, 109)] = ["Fire Cascading Necklace", "Wind Cascading Necklace", "Earth Cascading Necklace", "Water Cascading Necklace", "Void Cascading Necklace", "Holy Cascading Necklace"],
        [(110, 119)] = null,
        [(120, 149)] = [ "Fire Encrusted Necklace", "Wind Encrusted Necklace", "Earth Encrusted Necklace", "Water Encrusted Necklace", "Void Encrusted Necklace", "Holy Encrusted Necklace"],
        [(150, 179)] = ["Fire Diamat Necklace", "Wind Diamat Necklace", "Earth Diamat Necklace", "Water Diamat Necklace", "Void Diamat Necklace", "Holy Diamat Necklace"],
        [(180, 200)] = ["Children's Crux", "Fervent Mala"],
        [(201, 225)] = ["Merlin's Pendant"],
        [(226, 249)] = ["Violet Fire Festoon", "Violet Wind Festoon", "Violet Earth Festoon", "Violet Water Festoon", "Violet Void Festoon", "Violet Holy Festoon"],
        [(250, 299)] = ["Violet Fire Festoon", "Violet Wind Festoon", "Violet Earth Festoon", "Violet Water Festoon", "Violet Void Festoon", "Violet Holy Festoon"],
        [(300, 349)] = ["Void Red Plamit Necklace", "Holy Red Plamit Necklace"],
        [(350, 399)] = ["Violet Fire Matinee", "Violet Wind Matinee", "Violet Earth Matinee", "Violet Water Matinee"],
        [(400, 449)] = ["Violet Void Matinee", "Violet Holy Matinee"],
        [(450, 500)] = ["Turquoise Fire Matinee", "Turquoise Wind Matinee", "Turquoise Earth Matinee", "Turquoise Water Matinee"],
        [(501, 550)] = ["Turquoise Void Matinee", "Turquoise Holy Matinee"],
        [(551, 600)] = ["Ruby Void Locket", "Ruby Holy Locket"],
        [(601, 650)] = ["Sapphire Void Festoon", "Sapphire Holy Festoon"],
        [(651, 700)] = ["Ice Dragon's Fire Lariat", "Ice Dragon's Wind Lariat", "Ice Dragon's Earth Lariat", "Ice Dragon's Water Lariat", "Ice Dragon's Void Lariat", "Ice Dragon's Holy Lariat"],
        [(701, 750)] = ["Skull Dragon's Void Lariat", "Skull Dragon's Holy Lariat"],
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
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
        [(450, 500)] = null,
        [(501, 550)] = null,
        [(551, 600)] = null,
        [(601, 650)] = null,
        [(651, 700)] = null,
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected internal static readonly Dictionary<(int, int), List<string>> ShieldDrops = new()
    {
        [(1, 5)] = ["Wooden Shield"],
        [(6, 11)] = ["Floga Wooden Shield", "Thalassa Wooden Shield", "Zephyr Wooden Shield", "Laspi Wooden Shield", "Agios Wooden Shield", "Skia Wooden Shield", "Leather Shield"],
        [(12, 25)] = ["Floga Leather Shield", "Thalassa Leather Shield", "Zephyr Leather Shield", "Laspi Leather Shield", "Agios Leather Shield", "Skia Leather Shield", "Bronze Shield"],
        [(26, 38)] = ["Floga Bronze Shield", "Thalassa Bronze Shield", "Zephyr Bronze Shield", "Laspi Bronze Shield", "Agios Bronze Shield", "Skia Bronze Shield", "Etched Shield"],
        [(39, 43)] = ["Floga Etched Shield", "Thalassa Etched Shield", "Zephyr Etched Shield", "Laspi Etched Shield", "Agios Etched Shield", "Skia Etched Shield"],
        [(44, 54)] = ["Mythril Shield"],
        [(55, 65)] = ["Floga Mythril Shield", "Thalassa Mythril Shield", "Zephyr Mythril Shield", "Laspi Mythril Shield", "Agios Mythril Shield", "Skia Mythril Shield", "Hy-Brasyl Shield"],
        [(66, 76)] = ["Hy-Brasyl Shield"],
        [(77, 87)] = ["Floga Hy-Brasyl Shield", "Thalassa Hy-Brasyl Shield", "Zephyr Hy-Brasyl Shield", "Laspi Hy-Brasyl Shield", "Agios Hy-Brasyl Shield", "Skia Hy-Brasyl Shield"],
        [(88, 99)] = ["Talos Shield"],
        [(100, 109)] = ["Floga Talos Shield", "Thalassa Talos Shield", "Zephyr Talos Shield", "Laspi Talos Shield", "Agios Talos Shield", "Skia Talos Shield"],
        [(110, 119)] = null,
        [(120, 149)] = ["Talgonite Shield", "Floga Talgonite Shield", "Thalassa Talgonite Shield", "Zephyr Talgonite Shield", "Laspi Talgonite Shield", "Agios Talgonite Shield", "Skia Talgonite Shield"],
        [(150, 179)] = ["Blood Shield", "Floga Blood Shield", "Thalassa Blood Shield", "Zephyr Blood Shield", "Laspi Blood Shield", "Agios Blood Shield", "Skia Blood Shield"],
        [(180, 200)] = null,
        [(201, 225)] = null,
        [(226, 249)] = null,
        [(250, 299)] = null,
        [(300, 349)] = null,
        [(350, 399)] = null,
        [(400, 449)] = null,
        [(450, 500)] = null,
        [(501, 550)] = null,
        [(551, 600)] = null,
        [(601, 650)] = null,
        [(651, 700)] = null,
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    protected static readonly Dictionary<(int, int), List<string>> WristDrops = new()
    {
        [(1, 5)] = null,
        [(6, 15)] = ["Leather Bracer"],
        [(16, 20)] = ["Leather Bracer", "Iron Bracer"],
        [(21, 32)] = ["Iron Bracer"],
        [(33, 43)] = ["Chain Bracer"],
        [(44, 54)] = ["Signet Bracer", "Chain Bracer"],
        [(55, 65)] = ["Signet Bracer"],
        [(66, 76)] = ["Spiked Bracer"],
        [(77, 87)] = ["Spiked Bracer"],
        [(88, 99)] = null,
        [(100, 109)] = ["Hybrasyl Bracer"],
        [(110, 119)] = ["Hybrasyl Bracer"],
        [(120, 149)] = null,
        [(150, 179)] = ["Scale Bracer"],
        [(180, 200)] = ["Scale Bracer"],
        [(201, 225)] = null,
        [(226, 249)] = ["Juggernaut Glove"],
        [(250, 299)] = ["Talon Gauntlet"],
        [(300, 349)] = ["Golden Talon Gauntlet"],
        [(350, 399)] = ["Jade Bracer"],
        [(400, 449)] = ["Enchanted Jade Bracer"],
        [(450, 500)] = ["Empowered Jade Bracer"],
        [(501, 550)] = ["Sapphire Hybrasyl Bracer"],
        [(551, 600)] = null,
        [(601, 650)] = ["Obsidian Chained Bracer"],
        [(651, 700)] = ["Golden Obsidian Chained Bracer"],
        [(701, 750)] = null,
        [(751, 800)] = null,
        [(801, 850)] = null,
        [(851, 900)] = null,
        [(901, 950)] = null,
        [(951, 1000)] = null
    };

    // Reward based on Monster level
    protected static IEnumerable<string> GenerateDropsBasedOnLevel(Monster monster, Dictionary<(int, int), List<string>> dropsDict)
    {
        return monster == null ? null : (from entry in dropsDict where monster.Level >= entry.Key.Item1 && monster.Level <= entry.Key.Item2 select entry.Value).FirstOrDefault();
    }

    // Reward based on Player level
    protected internal static IEnumerable<string> GenerateDropsCailFountain(WorldClient client, Dictionary<(int, int), List<string>> dropsDict)
    {
        return client.Aisling == null ? null : (from entry in dropsDict where client.Aisling.Level >= entry.Key.Item1 && client.Aisling.Level <= entry.Key.Item2 select entry.Value).FirstOrDefault();
    }
}