using System.Diagnostics;
using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Network.Components;

public class MonolithComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 3_000;
    private const double TargetVisibleMonsters = 2.0;
    private const int ViewTileWidth = 13;
    private const int ViewTileHeight = 13;
    private const double DensityPerSpawnableTile = TargetVisibleMonsters / (ViewTileWidth * ViewTileHeight);

    protected internal override async Task Update()
    {
        var sw = Stopwatch.StartNew();
        var target = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            var elapsed = sw.Elapsed.TotalMilliseconds;
            if (elapsed < target)
            {
                var remaining = (int)(target - elapsed);
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 500));
                continue;
            }

            ManageSpawns();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private void ManageSpawns()
    {
        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            if (map == null || map.Height == 0 || map.Width == 0) continue;

            // Nodes
            PlaceNode(map);
            PlaceFlower(map);

            // Templates for this map only
            if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(map.ID, out var templates) || templates.Length == 0) continue;

            var monsters = SpriteQueryExtensions.MonstersOnMapSnapshot(map);

            // Map based overload guard
            var maxMonsters = CalculateMaxSprite(map);
            if (monsters.Count >= maxMonsters) continue;

            var countsByName = new Dictionary<string, int>(StringComparer.Ordinal);

            // Count existing monsters by template name
            foreach (var monster in monsters)
            {
                var name = monster?.Template?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                countsByName.TryGetValue(name, out var c);
                countsByName[name] = c + 1;
            }

            var remaining = maxMonsters - monsters.Count;

            foreach (var template in templates)
            {
                if (remaining <= 0) break;

                countsByName.TryGetValue(template.Name, out var count);

                if (count >= template.SpawnMax) continue;
                if (!template.ReadyToSpawn()) continue;

                CreateFromTemplate(template, map);

                countsByName[template.Name] = count + 1;
                remaining--;
            }
        }
    }

    /// <summary>
    /// Calculates the maximum number of monsters that can spawn within the specified area based on the number of
    /// spawnable tiles and a predefined density factor.
    /// </summary>
    /// <remarks>The returned value is clamped to ensure that the number of monsters remains within reasonable
    /// limits for the given area size. This helps maintain balanced gameplay and prevents excessive or insufficient
    /// monster spawns in very large or small areas.</remarks>
    /// <param name="map">The area in which monsters can spawn. Must provide the number of spawnable tiles via the SpawnableTileCount
    /// property.</param>
    /// <returns>The maximum number of monsters allowed to spawn in the area, constrained to a minimum and maximum value
    /// determined by the spawnable tile count.</returns>
    private static int CalculateMaxSprite(Area map)
    {
        var spawnable = map.SpawnableTileCount;
        var ideal = Math.Round(spawnable * DensityPerSpawnableTile);

        // Sanity Clamps
        var min = Math.Max(5, spawnable / 500); // ex: 10,000 tiles = 20 min
        var max = Math.Max(min, spawnable / 50); // ex: 10,000 tiles = 200 max

        return (int)Math.Clamp(ideal, min, max);
    }

    private static void CreateFromTemplate(MonsterTemplate template, Area map)
    {
        var newObj = Monster.Create(template, map);
        if (newObj != null)
            ObjectManager.AddObject(newObj);
    }

    /// <summary>
    /// Node placement
    /// </summary>
    private static void PlaceNode(Area map)
    {
        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Default)) return;
        // Maps that are small, do not spawn 
        if (map.Height <= 15 || map.Width <= 15) return;

        try
        {
            var ores = SpriteQueryExtensions.ItemsOnMapSnapshot(map).Where(i => OreNames.Contains(i.Template.Name));
            map.MiningNodesCount = ores.Count();

            var maxSprite = CalculateMaxSprite(map);
            var maxNode = CalculateMaxResourceNodes(maxSprite, 5) + 1;

            if (map.MiningNodesCount >= maxNode) return;

            var node = MiningNode(map);
            if (node == null) return;

            TryPlaceObjectRandomly(map, node, 10);
        }
        catch { }
    }

    private static readonly HashSet<string> OreNames = new(StringComparer.Ordinal)
    {
        "Raw Talos",
        "Raw Copper",
        "Raw Dark Iron",
        "Raw Hybrasyl",
        "Raw Cobalt Steel",
        "Raw Obsidian",
        "Chaos Ore"
    };

    private static Item MiningNode(Area map)
    {
        if (TryNode(map, MiningNodes.Talos, "Raw Talos", out var n1)) return n1;
        if (TryNode(map, MiningNodes.Copper, "Raw Copper", out var n2)) return n2;
        if (TryNode(map, MiningNodes.DarkIron, "Raw Dark Iron", out var n3)) return n3;
        if (TryNode(map, MiningNodes.Hybrasyl, "Raw Hybrasyl", out var n4)) return n4;
        if (TryNode(map, MiningNodes.CobaltSteel, "Raw Cobalt Steel", out var n5)) return n5;
        if (TryNode(map, MiningNodes.Obsidian, "Raw Obsidian", out var n6)) return n6;
        if (TryNode(map, MiningNodes.ChaosOre, "Chaos Ore", out var n7)) return n7;
        return null;
    }

    private static bool TryNode(Area map, MiningNodes flag, string itemName, out Item node)
    {
        node = null;
        if (!map.MiningNodes.MapNodeFlagIsSet(flag)) return false;
        if (Generator.RandomPercentPrecise() < .50) return false;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(itemName, out var template)) return false;
        node = new Item().Create(map, template);
        return node != null;
    }

    /// <summary>
    /// Flower placement
    /// </summary>
    private static void PlaceFlower(Area map)
    {
        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Default)) return;
        // Maps that are small, do not spawn 
        if (map.Height <= 15 || map.Width <= 15) return;

        try
        {
            var flowers = SpriteQueryExtensions.ItemsOnMapSnapshot(map).Where(i => FlowerNames.Contains(i.Template.Name));
            map.WildFlowersCount = flowers.Count();

            var maxSprite = CalculateMaxSprite(map);
            var maxNode = CalculateMaxResourceNodes(maxSprite, 7) + 1;

            if (map.WildFlowersCount >= maxNode) return;

            var node = FlowerNode(map);
            if (node == null) return;

            TryPlaceObjectRandomly(map, node, 10);
        }
        catch { }
    }

    private static readonly HashSet<string> FlowerNames = new(StringComparer.Ordinal)
    {
        "Gloom Bloom",
        "Betrayal Blossom",
        "Bocan Branch",
        "Cactus Lilium",
        "Prahed Bellis",
        "Aiten Bloom",
        "Reict Weed"
    };

    private static Item FlowerNode(Area map)
    {
        if (TryFlower(map, WildFlowers.GloomBloom, "Gloom Bloom", out var n1)) return n1;
        if (TryFlower(map, WildFlowers.Betrayal, "Betrayal Blossom", out var n2)) return n2;
        if (TryFlower(map, WildFlowers.Bocan, "Bocan Branch", out var n3)) return n3;
        if (TryFlower(map, WildFlowers.Cactus, "Cactus Lilium", out var n4)) return n4;
        if (TryFlower(map, WildFlowers.Prahed, "Prahed Bellis", out var n5)) return n5;
        if (TryFlower(map, WildFlowers.Aiten, "Aiten Bloom", out var n6)) return n6;
        if (TryFlower(map, WildFlowers.Reict, "Reict Weed", out var n7)) return n7;
        return null;
    }

    private static bool TryFlower(Area map, WildFlowers flag, string itemName, out Item node)
    {
        node = null;
        if (!map.WildFlowers.MapFlowerFlagIsSet(flag)) return false;
        if (Generator.RandomPercentPrecise() < .50) return false;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(itemName, out var template)) return false;
        node = new Item().Create(map, template);
        return node != null;
    }

    private static void TryPlaceObjectRandomly(Area map, Item node, int attempts)
    {
        for (var i = 0; i < attempts; i++)
        {
            var x = Generator.GenerateMapLocation(map.Height);
            var y = Generator.GenerateMapLocation(map.Width);
            if (map.IsWall(x, y)) continue;
            node.Pos = new Vector2(x, y);
            ObjectManager.AddObject(node);
            break;
        }
    }

    private static int CalculateMaxResourceNodes(int maxMonsters, int divisor, int min = 1)
    {
        if (maxMonsters <= 0) return 0;

        // 5 monsters -> mining = ceil(5/4)=2, flowers=ceil(5/6)=1
        var ideal = (int)Math.Ceiling(maxMonsters / (double)divisor);

        if (ideal < min) ideal = min;
        if (ideal > maxMonsters) ideal = maxMonsters;

        return ideal;
    }
}