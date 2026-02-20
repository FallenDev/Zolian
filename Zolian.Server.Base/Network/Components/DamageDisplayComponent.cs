using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class DamageDisplayComponent(WorldServer server) : WorldServerComponent(server)
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

            UpdateDamageCounter();

            var post = sw.Elapsed.TotalMilliseconds;
            var overshoot = post - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void UpdateDamageCounter()
    {
        Server.ForEachLoggedInAisling(static player =>
        {
            if (!player.GameSettings.DmgNumbers) return;
            if (player.DamageCounter == 0) return;

            try
            {
                ShowDmg(player);
            }
            catch { }
        });
    }

    private static void ShowDmg(Aisling aisling)
    {
        aisling.Client.SendPublicMessage(aisling.Serial, PublicMessageType.Chant, $"{aisling.DamageCounter}");
        ShowDmgTierAnimation(aisling);
        aisling.DamageCounter = 0;
    }

    private static void ShowDmgTierAnimation(Aisling aisling)
    {
        switch (aisling.DamageCounter)
        {
            case >= 1000000 and < 5000000: // 1M
                aisling.SendAnimationNearby(405, aisling.Position);
                break;
            case >= 5000000 and < 10000000: // 5M
                aisling.SendAnimationNearby(406, aisling.Position);
                break;
            case >= 10000000 and < 20000000: // 10M
                aisling.SendAnimationNearby(407, aisling.Position);
                break;
            case >= 20000000 and < 50000000: // 20M
                aisling.SendAnimationNearby(408, aisling.Position);
                break;
            case >= 50000000 and < 100000000: // 50M
                aisling.SendAnimationNearby(409, aisling.Position);
                break;
            case >= 100000000 and < 500000000: // 100M
                aisling.SendAnimationNearby(410, aisling.Position);
                break;
            case >= 500000000 and < 1000000000: // 500M
                aisling.SendAnimationNearby(411, aisling.Position);
                break;
            case >= 1000000000 and < 2000000000: // 1B
                aisling.SendAnimationNearby(412, aisling.Position);
                break;
            case >= 2000000000: // 2B
                aisling.SendAnimationNearby(413, aisling.Position);
                break;
            default:
                return;
        }
    }
}