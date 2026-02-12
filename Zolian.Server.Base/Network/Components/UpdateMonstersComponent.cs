using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class UpdateMonstersComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 200;

    protected internal override async Task Update()
    {
        const int tickMs = ComponentSpeed;

        // Monotonic timestamps
        var lastTick = Stopwatch.GetTimestamp();
        var nextTick = lastTick + MsToTicks(tickMs);

        while (ServerSetup.Instance.Running)
        {
            var now = Stopwatch.GetTimestamp();

            // Sleep until it's time (in small chunks to keep responsiveness)
            if (now < nextTick)
            {
                var remainingMs = TicksToMs(nextTick - now);
                if (remainingMs > 0)
                    await Task.Delay(Math.Min(remainingMs, 50)).ConfigureAwait(false);

                continue;
            }

            // Actual dt since last tick (monotonic)
            var dt = Stopwatch.GetElapsedTime(lastTick, now);
            lastTick = now;

            // Clamp dt to avoid huge steps after pauses / debugger / GC / hitch
            dt = ClampDt(dt, TimeSpan.FromMilliseconds(tickMs * 2));

            UpdateMonsterRoutine(dt);

            // Schedule next tick; if we're behind by multiple ticks, skip ahead (no catch-up spiral)
            nextTick += MsToTicks(tickMs);
            now = Stopwatch.GetTimestamp();

            if (now > nextTick + MsToTicks(tickMs))
            {
                // We're more than 1 tick behind; resync to "now + tick"
                nextTick = now + MsToTicks(tickMs);
            }
        }
    }

    private void UpdateMonsterRoutine(TimeSpan dt)
    {
        var now = DateTime.UtcNow;

        foreach (var mapKvp in ServerSetup.Instance.GlobalMapCache)
        {
            var map = mapKvp.Value;

            // Only update monsters on maps that have players -- 30 min delay after players leave
            if (!MapActivityGate.ShouldUpdateMonsters(map)) continue;

            var monstersById = ObjectManager.GetObjects<Monster>(map, m => !m.Skulled);
            if (monstersById.IsEmpty)
                continue;

            foreach (var kv in monstersById)
            {
                var monster = kv.Value;
                ProcessMonster(monster, dt, now);
            }
        }
    }

    private static void ProcessMonster(Monster monster, TimeSpan elapsedTime, DateTime now)
    {
        if (monster == null) return;

        if (monster.CurrentHp <= 0)
        {
            monster.Skulled = true;
            if (monster.Target is Aisling aisling)
                monster.AIScript?.OnDeath(aisling.Client);
            else
                monster.AIScript?.OnDeath();
            return;
        }

        monster.AIScript?.Update(elapsedTime);
        monster.LastUpdated = now;

        // Handle buffs and debuffs
        if (!monster.MonsterBuffAndDebuffStopWatch.IsRunning)
            monster.MonsterBuffAndDebuffStopWatch.Start();

        if (monster.MonsterBuffAndDebuffStopWatch.Elapsed.TotalMilliseconds < 1000) return;
        monster.UpdateBuffs(monster);
        monster.UpdateDebuffs(monster);
        monster.MonsterBuffAndDebuffStopWatch.Restart();
    }

    private static TimeSpan ClampDt(TimeSpan dt, TimeSpan max) => dt < TimeSpan.Zero ? TimeSpan.Zero : (dt > max ? max : dt);
    private static long MsToTicks(int ms) => (long)(Stopwatch.Frequency * (ms / 1000.0));
    private static int TicksToMs(long ticks) => (int)(ticks * 1000.0 / Stopwatch.Frequency);
}