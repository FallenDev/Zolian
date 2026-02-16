using System.Diagnostics;

using Darkages.Database;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;

namespace Darkages.Network.Components;

public class UpdateMapsComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 100;

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
                    await Task.Delay(Math.Min(remainingMs, 20)).ConfigureAwait(false);

                continue;
            }

            // Actual dt since last tick (monotonic)
            var dt = Stopwatch.GetElapsedTime(lastTick, now);
            lastTick = now;

            // Clamp dt to avoid huge steps after pauses / debugger / GC / hitch
            dt = ClampDt(dt, TimeSpan.FromMilliseconds(tickMs * 2));

            UpdateMapsRoutine(dt);

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

    private static void UpdateMapsRoutine(TimeSpan dt)
    {
        try
        {
            foreach (var kvp in ServerSetup.Instance.GlobalMapCache)
            {
                try
                {
                    kvp.Value?.Update(dt);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    continue;
                }
            }
        }
        catch
        {
            SentrySdk.CaptureMessage($"Map failed to update; Reload Maps initiated: {DateTime.UtcNow}");

            // Ensure only one reload routine runs at a time and avoid races with readers
            lock (ServerSetup.Instance.Game.MapReloadLock)
            {
                try
                {
                    // Take a snapshot of current mundanes and remove them safely.
                    var mundanesSnapshot = ServerSetup.Instance.GlobalMundaneCache?.Values.ToArray() ?? Array.Empty<Mundane>();
                    foreach (var npc in mundanesSnapshot)
                    {
                        try
                        {
                            ObjectManager.DelObject(npc);
                        }
                        catch (Exception delEx)
                        {
                            SentrySdk.CaptureException(delEx);
                        }
                    }

                    // Try to clear caches in a best-effort, safe manner.
                    // Use dynamic invocation so this code will attempt Clear() on
                    // common collection types (ConcurrentDictionary, Dictionary, etc.)
                    // and will not crash if Clear is not available.
                    try { (ServerSetup.Instance.TempGlobalMapCache as dynamic)?.Clear(); } catch { }
                    try { (ServerSetup.Instance.TempGlobalWarpTemplateCache as dynamic)?.Clear(); } catch { }
                    try { (ServerSetup.Instance.GlobalMundaneCache as dynamic)?.Clear(); } catch { }

                    // Clear player activity
                    MapActivityGate.ClearAll();

                    // Reload authoritative sources from the database
                    AreaStorage.Instance.CacheFromDatabase();
                    DatabaseLoad.CacheFromDatabase(new WarpTemplate());

                    // Notify connected players (snapshot to avoid modifying collection during iteration)
                    var formatted = "{=qSelf-Heal Routine Invokes Reload Maps";

                    Server.ForEachLoggedInAisling(state: formatted,
                        action: static (player, msg) =>
                        {
                            try
                            {
                                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, msg);
                                player.Client.ClientRefreshed();
                            }
                            catch { }
                        });
                }
                catch (Exception reloadEx)
                {
                    SentrySdk.CaptureException(reloadEx);
                }
            }
        }
    }

    private static TimeSpan ClampDt(TimeSpan dt, TimeSpan max) => dt < TimeSpan.Zero ? TimeSpan.Zero : (dt > max ? max : dt);
    private static long MsToTicks(int ms) => (long)(Stopwatch.Frequency * (ms / 1000.0));
    private static int TicksToMs(long ticks) => (int)(ticks * 1000.0 / Stopwatch.Frequency);
}