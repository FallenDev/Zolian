using System.Diagnostics;
using Darkages.Network.Server;
using Darkages.Sprites;

namespace Darkages.Network.Components;

public class PlayerStatusBarAndThreatComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 1000;

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
                    await Task.Delay(Math.Min(remaining, 100));

                continue;
            }

            UpdatePlayerStatusBarAndThreat();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            if (overshoot > 0 && overshoot < ComponentSpeed)
            {
                // Slightly compensate next tick if we ran late
                target = ComponentSpeed - (int)overshoot;
            }
            else
            {
                target = ComponentSpeed;
            }

            sw.Restart();
        }
    }

    private void UpdatePlayerStatusBarAndThreat()
    {
        try
        {
            foreach (var player in Server.Aislings)
            {
                if (player?.Client == null) continue;
                if (!player.LoggedIn) continue;
                ProcessUpdates(player);
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void ProcessUpdates(Aisling player)
    {
        player.UpdateBuffs(player);
        player.UpdateDebuffs(player);
        player.ThreatGeneratedSubsided(player);
    }
}
