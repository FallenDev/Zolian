using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

using System.Numerics;
using Darkages.Enums;

namespace Darkages.Network.Components;

public class MonolithComponent(WorldServer server) : WorldServerComponent(server)
{
    private static void CreateFromTemplate(MonsterTemplate template, Area map)
    {
        var newObj = Monster.Create(template, map);

        if (newObj == null) return;
        ServerSetup.Instance.GlobalMonsterCache[newObj.Serial] = newObj;
        Server.ObjectHandlers.AddObject(newObj);
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(ManageSpawns);
    }

    private static void ManageSpawns()
    {
        var templates = ServerSetup.Instance.GlobalMonsterTemplateCache;
        if (templates.Count == 0) return;

        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            if (map == null || map.Height == 0 || map.Width == 0) return;
            PlaceNode(map);

            var monstersOnMap = ServerSetup.Instance.GlobalMonsterCache.Count(i => i.Value.Map == map);
            if (monstersOnMap >= map.Height * map.Width / 100) continue;
            var temps = templates.Where(i => i.Value.AreaID == map.ID);

            foreach (var (_, monster) in temps)
            {
                var count = ServerSetup.Instance.GlobalMonsterCache.Count(i => i.Value.Template.Name == monster.Name);

                if (count >= monster.SpawnMax) continue;
                if (!monster.ReadyToSpawn()) continue;

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
        
        map.MiningNodesCount = Server.ObjectHandlers.GetObjects<Item>(map, i => i.Template is { Name: "Raw Dark Iron" } or { Name: "Raw Copper" } or { Name: "Raw Obsidian" }
            or { Name: "Raw Cobalt Steel" } or { Name: "Raw Hybrasyl" } or { Name: "Raw Talos" }).Count();

        if (map.MiningNodesCount >= map.Height * map.Width / 100) return;

        try
        {
            var node = MiningNode(map);
            if (node == null) return;
            var x = Generator.GenerateMapLocation(map.Height);
            var y = Generator.GenerateMapLocation(map.Width);

            for (var i = 0; i < 10; i++)
            {
                if (map.IsWall(x, y)) continue;
                node.Pos = new Vector2(x, y);

                Server.ObjectHandlers.AddObject(node);
                break;
            }
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
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
}