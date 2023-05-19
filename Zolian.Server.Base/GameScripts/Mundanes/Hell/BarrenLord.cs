using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Hell;

[Script("Barren Lord")]
public class BarrenLord : MundaneScript
{
    public BarrenLord(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        if (Mundane.WithinEarShotOf(client.Aisling))
        {
            TopMenu(client);
        }
    }

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>
        {
            new (0x0001, "Yes, Lord Barren"),
            new (0x0002, "No.")
        };

        client.SendOptionsDialog(Mundane, "You seek redemption?", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        if (!Mundane.WithinEarShotOf(client.Aisling)) return;

        switch (responseID)
        {
            case 0x0001:
                client.SendOptionsDialog(Mundane, "You dare pay the costs?",
                    new OptionsDataItem(0x0005, "Yes"),
                    new OptionsDataItem(0x0002, "No"));
                break;
            case 0x0002:
                client.CloseDialog();
                break;
        }

        if (responseID != 0x0005) return;
        client.Aisling.BaseHp -= ServerSetup.Instance.Config.DeathHPPenalty;

        if (client.Aisling.MaximumHp <= ServerSetup.Instance.Config.MinimumHp)
            client.Aisling.BaseHp = ServerSetup.Instance.Config.MinimumHp;

        client.Revive();
        client.SendMessage(0x02, "You have lost some health.");
        client.SendStats(StatusFlags.MultiStat);
        client.TransitionToMap(136, new Position(4, 7));
        Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(304); });
        client.CloseDialog();
    }
}