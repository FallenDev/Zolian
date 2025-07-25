using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Cid")]
public class Cid(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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
            new(0x01, "Eastern Woodlands"),
            new(0x02, "Abel Beach"),
            new(0x03, "Mehadi Swamp"),
            new(0x04, "Loures Harbor"),
            new(0x05, "Karlopos Beach"),
            new(0x06, "Shinewood Forest"),
            new(0x07, "Arena")
        };

        client.SendOptionsDialog(Mundane, "Hello there?! I'm a local merchant that travels often and have room in my carriage, need a lift?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x01:
                {
                    client.TransitionToMap(600, new Position(13, 20));
                    break;
                }
            case 0x02:
                {
                    client.TransitionToMap(502, new Position(55, 63));
                    break;
                }
            case 0x03:
                {
                    client.TransitionToMap(3071, new Position(4, 13));
                    break;
                }
            case 0x04:
                {
                    client.TransitionToMap(6925, new Position(24, 14));
                    break;
                }
            case 0x05:
                {
                    client.TransitionToMap(4720, new Position(14, 19));
                    break;
                }
            case 0x06:
                {
                    client.TransitionToMap(542, new Position(14, 15));
                    break;
                }
            case 0x07:
                {
                    client.TransitionToMap(5232, new Position(5, 9));
                    break;
                }
        }

        client.CloseDialog();
        client.Aisling.SendAnimationNearby(199, null, client.Aisling.Serial);
    }

    public override void OnGossip(WorldClient client, string message) { }
}