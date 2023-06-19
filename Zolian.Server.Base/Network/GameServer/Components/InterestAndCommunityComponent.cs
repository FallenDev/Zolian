using System.Globalization;

using Darkages.Infrastructure;

namespace Darkages.Network.GameServer.Components;

public class InterestAndCommunityComponent : GameServerComponent
{
    private readonly GameServerTimer _timer = new(TimeSpan.FromSeconds(45));
    private readonly GameServerTimer _interest = new(TimeSpan.FromMinutes(30));

    public InterestAndCommunityComponent(Server.GameServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(SaveCommunity);
        if (_interest.Update(elapsedTime)) ZolianUpdateDelegate.Update(AccrueInterest);
    }

    private static void SaveCommunity()
    {
        if (ServerSetup.Instance.Game == null || ServerSetup.Instance.Game.Clients == null) return;
        ServerSetup.SaveCommunityAssets();
    }

    private static void AccrueInterest()
    {
        if (!ServerSetup.Instance.Running || ServerSetup.Instance.Game.Clients == null) return;

        foreach (var client in ServerSetup.Instance.Game.Clients.Values.Where(client => client is { Aisling: not null }))
        {
            if (!client.Aisling.LoggedIn) continue;
            if (client.Aisling.BankManager == null) continue;
            if (client.Aisling.BankedGold <= 0)
            {
                client.Aisling.BankedGold = 0;
                continue;
            }

            var calc = Math.Round(client.Aisling.BankedGold * 0.00777).ToString(CultureInfo.CurrentCulture);
            var interest = (uint)Math.Round(client.Aisling.BankedGold * 0.00777);
            if (client.Aisling.BankedGold + interest >= uint.MaxValue)
            {
                client.SendMessage(0x03, $"{{=uKing Bruce wishes to see you. - No interest gained -");
                continue;
            }
            client.SendMessage(0x03, $"{{=uInterest Accrued: {calc}");
            client.Aisling.BankedGold += interest;
        }
    }
}