using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Hell;

[Script("Barren Lord")]
public class BarrenLord(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new (0x0001, "Yes, Lord Barren"),
            new (0x0002, "No.")
        };

        client.SendOptionsDialog(Mundane, "You seek redemption?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x0001:
                client.SendOptionsDialog(Mundane, "You dare pay the costs?",
                    new Dialog.OptionsDataItem(0x0005, "Yes"),
                    new Dialog.OptionsDataItem(0x0002, "No"));
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
        client.SendServerMessage(ServerMessageType.OrangeBar1, "You have lost some health.");
        client.SendAttributes(StatUpdateType.Full);
        client.TransitionToMap(136, new Position(4, 7));
        Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(304, null, client.Aisling.Serial)); });
        client.CloseDialog();
    }
}