using System.Collections.Frozen;
using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class MundaneComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 10_000;

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

            SpawnMundanes();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private void SpawnMundanes()
    {
        foreach (var template in ServerSetup.Instance.GlobalMundaneTemplateCache.Values)
        {
            if (template == null) continue;
            if (template.AreaID == 0) continue;

            // Map must exist and be ready
            if (!ServerSetup.Instance.GlobalMapCache.TryGetValue(template.AreaID, out var map)) continue;
            if (!map.Ready) continue;

            // Check if a matching Mundane already exists and is alive
            var existing = ObjectManager.GetObject<Mundane>(
                map,
                m => m.CurrentMapId == map.ID &&
                     m.Template?.Name == template.Name &&
                     m.CurrentHp > 0);

            if (existing != null) continue;
            Mundane.Create(template);
        }

        // Update the MundaneByMapCache
        if (ServerSetup.Instance.TempMundaneByMapCache.Count == 0) return;
        ServerSetup.Instance.MundaneByMapCache = ServerSetup.Instance.TempMundaneByMapCache.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        ServerSetup.Instance.TempMundaneByMapCache.Clear();
        ServerSetup.EventsLogger($"Mundanes Spawned: {ServerSetup.Instance.MundaneByMapCache.Count}");
    }
}