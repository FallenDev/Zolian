using System.Diagnostics;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class ClientCreationLimit(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 3_600_000;

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
                    await Task.Delay(Math.Min(remaining, 60_000));
                continue;
            }

            CleanupCreationLimits();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void CleanupCreationLimits()
    {
        if (!ServerSetup.Instance.Running) return;

        var dict = ServerSetup.Instance.GlobalCreationCount;

        foreach (var kvp in dict)
        {
            var ip = kvp.Key;
            var count = kvp.Value;

            if (count == 0)
            {
                dict.TryRemove(ip, out _);
                continue;
            }

            // Compute the decremented value safely
            byte newCount = (byte)(count - 1);
            dict.TryUpdate(ip, newCount, count);

            // If we reached zero after decrement, remove the entry
            if (newCount == 0)
                dict.TryRemove(ip, out _);
        }
    }
}