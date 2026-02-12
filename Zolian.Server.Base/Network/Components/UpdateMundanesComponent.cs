using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class UpdateMundanesComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 1500;

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
                    await Task.Delay(Math.Min(remainingMs, 150)).ConfigureAwait(false);

                continue;
            }

            // Actual dt since last tick (monotonic)
            var dt = Stopwatch.GetElapsedTime(lastTick, now);
            lastTick = now;

            // Clamp dt to avoid huge steps after pauses / debugger / GC / hitch
            dt = ClampDt(dt, TimeSpan.FromMilliseconds(tickMs * 2));

            UpdateMundanesRoutine(dt);

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

    private void UpdateMundanesRoutine(TimeSpan dt)
    {
        var now = DateTime.UtcNow;

        foreach (var mapKvp in ServerSetup.Instance.GlobalMapCache)
        {
            var map = mapKvp.Value;

            var mundanesById = ObjectManager.GetObjects<Mundane>(map, m => m != null);
            if (mundanesById.IsEmpty)
                continue;

            foreach (var kv in mundanesById)
            {
                var mundane = kv.Value;
                ProcessMundane(mundane, dt, now);
            }
        }
    }

    private static void ProcessMundane(Mundane mundane, TimeSpan elapsedTime, DateTime now)
    {
        if (mundane == null) return;
        mundane.Update(elapsedTime);
        mundane.LastUpdated = now;
    }

    private static TimeSpan ClampDt(TimeSpan dt, TimeSpan max) => dt < TimeSpan.Zero ? TimeSpan.Zero : (dt > max ? max : dt);
    private static long MsToTicks(int ms) => (long)(Stopwatch.Frequency * (ms / 1000.0));
    private static int TicksToMs(long ticks) => (int)(ticks * 1000.0 / Stopwatch.Frequency);
}