using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Arena;

[Script("VoidSphereWarp")]
public class VoidSphereWarp(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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
            new(0x01, "Reach for the Crystal")
        };

        client.SendOptionsDialog(Mundane, "You see a shimmering Crystal, fluctuating with madness; Touch it? ", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != 1500)
        {
            client.Dispose();
            return;
        }

        if (responseID != 0x01) return;
        var rand = Generator.RandNumGen100();

        switch (rand)
        {
            case <= 24:
                client.TransitionToMap(1501, new Position(13, 15));
                break;
            case <= 49:
                client.TransitionToMap(1501, new Position(13, 15));
                break;
            case <= 74:
                client.TransitionToMap(1501, new Position(13, 15));
                break;
            case <= 100:
                client.TransitionToMap(1501, new Position(13, 15));
                break;
            default:
                client.TransitionToMap(1501, new Position(13, 15));
                break;
        }

        client.CloseDialog();
        client.SendAnimation(240, null, client.Aisling.Serial);
        client.SendServerMessage(ServerMessageType.ActiveMessage, "The void pulls you in.. and spits you out");
    }
}