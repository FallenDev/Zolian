using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PingComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 7000;

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
                    await Task.Delay(Math.Min(remaining, 50));
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
        foreach (var player in Server.Aislings)
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;

            try
            {
                player.Client.SendHeartBeat(0x20, 0x14);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }
    }
}