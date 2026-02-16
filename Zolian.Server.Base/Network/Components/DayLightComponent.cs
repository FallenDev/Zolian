using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class DayLightComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 15_000;

    private static readonly (byte start, byte end)[] Routine =
    {
        (0, 0),
        (0, 1),
        (1, 2),
        (2, 3),
        (3, 4),
        (4, 5),
        (5, 4),
        (4, 3),
        (3, 2),
        (2, 1),
        (1, 0),
        (0, 0)
    };

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

            UpdateDayLight();

            var post = sw.Elapsed.TotalMilliseconds;
            var overshoot = post - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void UpdateDayLight()
    {
        // Wrap phase 0–11
        if (ServerSetup.Instance.LightPhase >= 12)
            ServerSetup.Instance.LightPhase = 0;

        var (start, end) = Routine[ServerSetup.Instance.LightPhase];

        // Only move to the end state if we're still at "start"
        if (ServerSetup.Instance.LightLevel == start)
            ServerSetup.Instance.LightLevel = end;

        Server.ForEachLoggedInAisling(static player =>
        {
            try
            {
                player.Client.SendLightLevel((LightLevel)ServerSetup.Instance.LightLevel);
            }
            catch { }
        });

        ServerSetup.Instance.LightPhase++;
    }
}