using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Oren;

[Script("IsleShopPort")]
public class IsleShopPort(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.CurrentMapId == 6600)
        {
            options.Add(new(0x01, $"Guide me to the {{=qShop"));
            client.SendOptionsDialog(Mundane, "She's eccentric and likes to live out in the wild, but I'll take you to him.", options.ToArray());
        }
        else
        {
            options.Add(new(0x02, $"Safe Passage back"));
            client.SendOptionsDialog(Mundane, "I'm resting, but if you want passage back out. I'll guide you.", options.ToArray());
        }
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x01:
                {
                    client.TransitionToMap(6623, new Position(9, 5));
                    break;
                }
            case 0x02:
                {
                    client.TransitionToMap(6600, new Position(16, 13));
                    break;
                }
        }

        client.CloseDialog();
        client.Aisling.SendAnimationNearby(199, null, client.Aisling.Serial);
    }

    public override void OnGossip(WorldClient client, string message) { }
}