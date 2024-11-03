using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Templates;
using Darkages.Types;
using System.Numerics;
using Darkages.Enums;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class MonolithComponent(WorldServer server) : WorldServerComponent(server)
{
    private static void CreateFromTemplate(MonsterTemplate template, Area map)
    {
        var newObj = Monster.Create(template, map);

        if (newObj == null) return;
        ObjectManager.AddObject(newObj);
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(ManageSpawns);
    }

    private static void ManageSpawns()
    {
        if (ServerSetup.Instance.GlobalMonsterTemplateCache.Count == 0) return;

        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            if (map == null || map.Height == 0 || map.Width == 0) return;
            PlaceNode(map);
            PlaceFlower(map);

            // Ensure the map isn't overloaded with monsters
            var monstersOnMap = ObjectManager.GetObjects<Monster>(map, m => m.IsAlive).Count();
            if (monstersOnMap >= map.Height * map.Width / 100) continue;

            // Check each map for monster SpawnMax, and whether it's ready to spawn
            foreach (var (_, monster) in ServerSetup.Instance.GlobalMonsterTemplateCache.Where(i => i.Value.AreaID == map.ID))
            {
                var count = ObjectManager.GetObjects<Monster>(map, m => m.IsAlive || m.Template.MonsterRace == MonsterRace.Dummy)
                    .Count(i => i.Template.Name == monster.Name);

                if (count >= monster.SpawnMax) continue;
                if (!monster.ReadyToSpawn()) continue;

                // Spawn from template
                CreateFromTemplate(monster, map);
            }
        }
    }

    /// <summary>
    /// Logic to check map for number of nodes on it, then place the node
    /// </summary>
    private static void PlaceNode(Area map)
    {
        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Default)) return;
        if (map.Height < 15 || map.Width < 15) return;

        try
        {
            map.MiningNodesCount = ObjectManager.GetObjects<Item>(map, i => i is
            {
                Template: { Name: "Raw Dark Iron" } or { Name: "Raw Copper" } or { Name: "Raw Obsidian" }
                or { Name: "Raw Cobalt Steel" } or { Name: "Raw Hybrasyl" } or { Name: "Raw Talos" }
            }).Count();

            if (map.MiningNodesCount >= map.Height * map.Width / 200) return;

            var node = MiningNode(map);
            if (node == null) return;
            var x = Generator.GenerateMapLocation(map.Height);
            var y = Generator.GenerateMapLocation(map.Width);

            for (var i = 0; i < 10; i++)
            {
                if (map.IsWall(x, y)) continue;
                node.Pos = new Vector2(x, y);

                ObjectManager.AddObject(node);
                break;
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Logic to check what nodes can populate on a map, create them, then return them randomly
    /// </summary>
    private static Item MiningNode(Area map)
    {

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Talos))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"]);
        }

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Copper))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Copper"]);
        }

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.DarkIron))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Dark Iron"]);
        }

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Hybrasyl))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Hybrasyl"]);
        }

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.CobaltSteel))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Cobalt Steel"]);
        }

        if (map.MiningNodes.MapNodeFlagIsSet(MiningNodes.Obsidian))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Obsidian"]);
        }

        return null;
    }

    /// <summary>
    /// Logic to check map for number of flowers on it
    /// </summary>
    private static void PlaceFlower(Area map)
    {
        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Default)) return;
        if (map.Height < 15 || map.Width < 15) return;

        try
        {
            map.WildFlowersCount = ObjectManager.GetObjects<Item>(map, i => i is
            {
                Template: { Name: "Gloom Bloom" } or { Name: "Betrayal Blossom" } or { Name: "Bocan Branch" }
                or { Name: "Cactus Lilium" } or { Name: "Prahed Bellis" } or { Name: "Aiten Bloom" } or { Name: "Reict Weed" }
            }).Count();

            if (map.WildFlowersCount >= 2) return;

            var node = FlowerNode(map);
            if (node == null) return;
            var x = Generator.GenerateMapLocation(map.Height);
            var y = Generator.GenerateMapLocation(map.Width);

            for (var i = 0; i < 10; i++)
            {
                if (map.IsWall(x, y)) continue;
                node.Pos = new Vector2(x, y);

                ObjectManager.AddObject(node);
                break;
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Logic to check what nodes can populate on a map, create them, then return them randomly
    /// </summary>
    private static Item FlowerNode(Area map)
    {

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.GloomBloom))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Gloom Bloom"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Betrayal))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Betrayal Blossom"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Bocan))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Bocan Branch"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Cactus))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Cactus Lilium"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Prahed))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Prahed Bellis"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Aiten))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Aiten Bloom"]);
        }

        if (map.WildFlowers.MapFlowerFlagIsSet(WildFlowers.Reict))
        {
            var nodeChance = Generator.RandomNumPercentGen();

            if (nodeChance >= .50)
                return new Item().Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Reict Weed"]);
        }

        return null;
    }
}