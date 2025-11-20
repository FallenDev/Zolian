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

            // Monsters
            var monstersOnMap = ObjectManager
                .GetObjects<Monster>(map, m => m.IsAlive)
                .Values
                .ToList();

            // Map based overload guard
            var maxMonsters = CalculateMaxMonsters(map);
            if (monstersOnMap.Count >= maxMonsters) continue;

            foreach (var template in ServerSetup.Instance.GlobalMonsterTemplateCache.Values)
            {
                if (template.AreaID != map.ID) continue;
                var count = monstersOnMap.Count(m => m.Template.Name == template.Name);
                if (count >= template.SpawnMax) continue;
                if (!template.ReadyToSpawn()) continue;

                CreateFromTemplate(template, map);
            }
        }
    }

    private static int CalculateMaxMonsters(Area map)
    {
        var area = map.Height * map.Width;

        // Monsters in a single 12x12 view on average
        const double targetVisible = 2.0;
        const int viewSize = 12 * 12;

        // Density = monsters per tile
        var density = targetVisible / viewSize;
        var ideal = area * density;

        // Sanity Clamps
        var min = Math.Max(5, area / 500);
        var max = Math.Max(min, area / 30);

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
        if (map.Height < 15 || map.Width < 15) return;

        try
        {
            map.MiningNodesCount = ObjectManager
                .GetObjects<Item>(map, i => i is
                {
                    Template: { Name: "Raw Dark Iron" } or { Name: "Raw Copper" } or { Name: "Raw Obsidian" }
                    or { Name: "Raw Cobalt Steel" } or { Name: "Raw Hybrasyl" } or { Name: "Raw Talos" }
                    or { Name: "Chaos Ore" }
                })
                .Count;

            if (map.MiningNodesCount >= 20) return;
            if (map.MiningNodesCount >= (map.Height * map.Width) / 200) return;

            var node = MiningNode(map);
            if (node == null) return;

            TryPlaceObjectRandomly(map, node, 10);
        }
        catch
        {
            // suppressed
        }
    }

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
        if (map.Height < 15 || map.Width < 15) return;

        try
        {
            map.WildFlowersCount = ObjectManager
                .GetObjects<Item>(map, i => i is
                {
                    Template: { Name: "Gloom Bloom" } or { Name: "Betrayal Blossom" } or { Name: "Bocan Branch" }
                    or { Name: "Cactus Lilium" } or { Name: "Prahed Bellis" } or { Name: "Aiten Bloom" }
                    or { Name: "Reict Weed" }
                })
                .Count;

            if (map.WildFlowersCount >= 2) return;

            var node = FlowerNode(map);
            if (node == null) return;

            TryPlaceObjectRandomly(map, node, 10);
        }
        catch
        {
            // suppressed
        }
    }

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
}