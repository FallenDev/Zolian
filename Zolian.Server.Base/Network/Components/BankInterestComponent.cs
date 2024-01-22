using Chaos.Common.Definitions;

using Darkages.Network.Server;

using System.Globalization;

namespace Darkages.Network.Components;

public class BankInterestComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(AccrueInterest);
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

            var calc = Math.Round(player.BankedGold * 0.00333).ToString(CultureInfo.CurrentCulture);
            var interest = (uint)Math.Round(player.BankedGold * 0.00333);
            if (interest >= 1000000)
                interest = 1000000;
            if (player.BankedGold + interest >= uint.MaxValue)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    $"{{=uBank Cap - No interest gained -");
                return;
            }

            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uInterest Accrued: {calc} coins");
            player.BankedGold += interest;
        });
    }
}