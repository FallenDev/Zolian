using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class BankInterestComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 1_800_000;

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

                // This is a long-interval task; coarse sleep is fine.
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 60_000));

                continue;
            }

            AccrueInterest();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void AccrueInterest()
    {
        Server.ForEachLoggedInAisling(static player =>
        {
            try
            {
                ApplyInterest(player);
            }
            catch { }
        });
    }

    private static void ApplyInterest(Aisling player)
    {
        if (player.BankedGold <= 0)
        {
            player.BankedGold = 0;
            return;
        }

        // ~0.333% interest
        var interest = (uint)Math.Round(player.BankedGold * 0.00333);

        // Cap interest per tick
        const uint maxInterest = 1_000_000;
        if (interest >= maxInterest)
            interest = maxInterest;

        // Prevent overflow on BankedGold + interest
        if (player.BankedGold >= ulong.MaxValue - interest)
        {
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=uBank Cap - No interest gained -");
            return;
        }

        player.BankedGold += interest;
        player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uInterest Accrued: {interest} coins");
    }
}