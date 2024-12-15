using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class MundaneComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 10000;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(500);
                continue;
            }

            SpawnMundanes();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static void SpawnMundanes()
    {
        foreach (var mundane in from mundane in ServerSetup.Instance.GlobalMundaneTemplateCache
                                where mundane.Value != null
                     && mundane.Value.AreaID != 0
                                where ServerSetup.Instance.GlobalMapCache.ContainsKey(mundane.Value.AreaID)
                                let map = ServerSetup.Instance.GlobalMapCache[mundane.Value.AreaID]
                                where map is { Ready: true }
                                let npc = ObjectManager.GetObject<Mundane>(map, i => i.CurrentMapId == map.ID
                                                                         && i.Template != null
                                                                         && i.Template.Name ==
                                                                         mundane.Value.Name)
                                where npc is not { CurrentHp: > 0 }
                                select mundane)
        {
            Mundane.Create(mundane.Value);
        }
    }
}