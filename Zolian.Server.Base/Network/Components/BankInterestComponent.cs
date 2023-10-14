using System.Globalization;
using Chaos.Common.Definitions;
using Darkages.Network.Server;

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

        foreach (var aisling in Server.Aislings.Where(player => player is not null))
        {
            if (!aisling.LoggedIn) continue;
            if (aisling.BankManager == null) continue;
            if (aisling.BankedGold <= 0)
            {
                aisling.BankedGold = 0;
                continue;
            }

            var calc = Math.Round(aisling.BankedGold * 0.00777).ToString(CultureInfo.CurrentCulture);
            var interest = (uint)Math.Round(aisling.BankedGold * 0.00777);
            if (aisling.BankedGold + interest >= uint.MaxValue)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uKing Bruce wishes to see you. - No interest gained -");
                continue;
            }
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=uInterest Accrued: {calc}");
            aisling.BankedGold += interest;
        }
    }
}