using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites;

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

            foreach (var player in Server.Aislings)
                Ping(player);

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            if (overshoot > 0)
            {
                // Compensate next tick by firing slightly earlier
                target = ComponentSpeed - (int)overshoot;
                if (target < 0)
                    target = 0;
            }
            else
            {
                target = ComponentSpeed;
            }

            sw.Restart();
        }
    }


    private static void Ping(Aisling player)
    {
        if (player?.Client == null) return;
        if (!player.LoggedIn) return;
        player.Client.SendHeartBeat(0x20, 0x14);
    }
}