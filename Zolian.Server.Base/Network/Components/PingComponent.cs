using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PingComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 5000;
    private const byte HeartbeatFirst = 0x14;
    private const byte HeartbeatSecond = 0x20;
    private static readonly TimeSpan HeartBeatTimeout = TimeSpan.FromSeconds(8);
    private const int MaxHeartBeatMisses = 3;

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

                // Clamp to avoid super tiny delays
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 10));
                continue;
            }

            Ping();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void Ping()
    {
        Server.ForEachLoggedInAisling(static player =>
        {
            try
            {
                var c = player.Client;
                if (c is null)
                    return;

                // If a heartbeat is in-flight, check for timeout
                if (Volatile.Read(ref c.HeartBeatInFlight) != 0)
                {
                    var start = Volatile.Read(ref c.HeartBeatStartTimestamp);

                    // If we somehow have in-flight but no start timestamp, treat it as timed out.
                    if (start <= 0)
                    {
                        Interlocked.Exchange(ref c.HeartBeatInFlight, 0);

                        if (Interlocked.Increment(ref c.HeartBeatMisses) >= MaxHeartBeatMisses)
                            c.CloseTransport();

                        return;
                    }

                    var now = Stopwatch.GetTimestamp();
                    // Still waiting on current heartbeat
                    if (Stopwatch.GetElapsedTime(start, now) < HeartBeatTimeout) return;

                    // Timed out => count as a miss
                    Interlocked.Exchange(ref c.HeartBeatInFlight, 0);

                    if (Interlocked.Increment(ref c.HeartBeatMisses) >= MaxHeartBeatMisses)
                    {
                        c.CloseTransport();
                        return;
                    }
                }

                c.SendHeartBeat(HeartbeatFirst, HeartbeatSecond);
            }
            catch { }
        });
    }
}