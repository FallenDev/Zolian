using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tagor;

[Script("Tagor Access")]
public class TagorAccess(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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
            client.Aisling.QuestManager.ArtursGift >= 1
                ? new Dialog.OptionsDataItem(0x02, "Necropolis Access")
                : new Dialog.OptionsDataItem(0x03, "Can I enter?")
        };

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level <= 120
                ? "No one enters without a royal decree!"
                : "Are you looking to die?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x02:
                client.TransitionToMap(1202, new Position(8, 10));
                client.CloseDialog();
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qTout: {{=aGood luck!");
                break;
            case 0x03:
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, $"{Mundane.Name}: Step away from the kings guard!"));
                client.CloseDialog();
                break;
        }
    }
}