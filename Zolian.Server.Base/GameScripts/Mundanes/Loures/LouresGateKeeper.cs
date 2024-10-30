using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Loures;

[Script("LouresGateKeeper")]
public class LouresGateKeeper(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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
            new(0x01, "To the Castle"),
            new(0x02, "To the Citadel"),
            new(0x03, "To the Goblin Gacha"),
            new(0x04, "To the Harbor")
        };

        client.SendOptionsDialog(Mundane, "Hail Citizen! How can I assist you? Perhaps an escort?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x01:
                {
                    client.TransitionToMap(3271, new Position(29, 43));
                    break;
                }
            case 0x02:
                {
                    client.TransitionToMap(3925, new Position(43, 47));
                    break;
                }
            case 0x03:
                {
                    client.TransitionToMap(3925, new Position(2, 95));
                    break;
                }
            case 0x04:
                {
                    client.TransitionToMap(6925, new Position(20, 32));
                    break;
                }
        }

        client.CloseDialog();
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(199, null, client.Aisling.Serial));
    }

    public override void OnGossip(WorldClient client, string message) { }
}