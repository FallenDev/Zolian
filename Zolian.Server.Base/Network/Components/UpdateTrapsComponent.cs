using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class UpdateTrapsComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 1000;

    protected internal override async Task Update()
    {
        const int tickMs = ComponentSpeed;
        const int jitterMs = 100;

        var lastTick = Stopwatch.GetTimestamp();

        while (ServerSetup.Instance.Running)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(lastTick, now);

            if (elapsed.TotalMilliseconds < tickMs)
            {
                var remaining = tickMs - (int)elapsed.TotalMilliseconds;

                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, jitterMs)).ConfigureAwait(false);

                continue;
            }

            UpdateTrapsRoutine();

            lastTick = Stopwatch.GetTimestamp();
        }
    }

    private static void UpdateTrapsRoutine()
    {
        var traps = ServerSetup.Instance.Traps;
        if (traps.IsEmpty) return;

        foreach (var kvp in traps)
        {
            try
            {
                kvp.Value?.Update();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                continue;
            }
        }
    }
}