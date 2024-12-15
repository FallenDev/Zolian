using System.Diagnostics;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class BankInterestComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 1800000;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(5000);
                continue;
            }

            AccrueInterest();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static void AccrueInterest()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        Parallel.ForEach(Server.Aislings, (player) =>
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;
            if (player.BankManager == null) return;
            if (player.BankedGold <= 0)
            {
                player.BankedGold = 0;
                return;
            }

            var interest = (uint)Math.Round(player.BankedGold * 0.00333);
            if (interest >= 1000000)
                interest = 1000000;
            if (player.BankedGold + interest >= ulong.MaxValue)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uBank Cap - No interest gained -");
                return;
            }

            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uInterest Accrued: {interest} coins");
            player.BankedGold += interest;
        });
    }
}