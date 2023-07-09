using System.Numerics;

using Darkages.Common;
using Darkages.Infrastructure;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Components;

public class MonolithComponent : WorldServerComponent
{
    private readonly WorldServerTimer _timer;

    public MonolithComponent(WorldServer server) : base(server)
    {
        _timer = new WorldServerTimer(TimeSpan.FromMilliseconds(ServerSetup.Instance.Config.GlobalSpawnTimer));
    }

    private void CreateFromTemplate(MonsterTemplate template, Area map)
    {
        var newObj = Monster.Create(template, map);

        if (newObj == null) return;
        ServerSetup.Instance.GlobalMonsterCache[newObj.Serial] = newObj;
        Server.ObjectHandlers.AddObject(newObj);
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime))
            ZolianUpdateDelegate.Update(ManageSpawns);
    }

    private void ManageSpawns()
    {
        var templates = ServerSetup.Instance.GlobalMonsterTemplateCache;
        if (templates.Count == 0) return;

        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            if (map == null || map.Rows == 0 || map.Cols == 0) return;

            var temps = templates.Where(i => i.Value.AreaID == map.ID);

            foreach (var (_, monster) in temps)
            {
                var count = ServerSetup.Instance.GlobalMonsterCache.Count(i => i.Value.Template.Name == monster.Name);

                if (!monster.ReadyToSpawn()) continue;
                if (count >= monster.SpawnMax) continue;
                if (count >= map.Rows * map.Cols / 6) continue;

                PlaceNode(map);
                CreateFromTemplate(monster, map);
            }
        }
    }

    private void PlaceNode(Area map)
    {
        if (map.Rows < 25 || map.Cols < 25) return;

        map.MiningNodes = Server.ObjectHandlers.GetObjects<Item>(map, i => i.Template is { Name: "Raw Dark Iron" } or { Name: "Raw Copper" } or { Name: "Raw Obsidian" }
            or { Name: "Raw Cobalt Steel" } or { Name: "Raw Hybrasyl" } or { Name: "Raw Talos" }).Count();

        if (map.MiningNodes >= 6) return;
        switch (map.ID)
        {
            case 3029:
            case 5257:
                return;
            default:
                try
                {
                    var node = MiningNode(map);
                    var x = Generator.GenerateMapLocation(map.Rows);
                    var y = Generator.GenerateMapLocation(map.Cols);

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

                break;
        }
    }

    private static Item MiningNode(Area map)
    {
        var qualityNode = Generator.RandomNumPercentGen();
        var item = new Item();

        if (map.Name.Contains("Crypt"))
        {
            return qualityNode switch
            {
                >= 0 and <= .50 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Copper"]),
                > .50 and <= 1 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"]),
                _ => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"])
            };
        }

        if (map.Name.Contains("Wood"))
        {
            return qualityNode switch
            {
                >= 0 and <= .32 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Copper"]),
                > .32 and <= .66 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Hybrasyl"]),
                > .66 and <= 1 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"]),
                _ => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"])
            };
        }

        if (map.ID == 623 || map.Name.Contains("Garden"))
        {
            return item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"]);
        }

        // ToDo: Make it so nodes have a chance to drop a gem like ruby that enhances weapon stats.

        return qualityNode switch
        {
            >= 0 and <= .16 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Dark Iron"]),
            > .16 and <= .32 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Copper"]),
            > .32 and <= .48 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Obsidian"]),
            > .48 and <= .66 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Cobalt Steel"]),
            > .66 and <= .84 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Hybrasyl"]),
            > .84 and <= 1 => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"]),
            _ => item.Create(map, ServerSetup.Instance.GlobalItemTemplateCache["Raw Talos"])
        };
    }
}