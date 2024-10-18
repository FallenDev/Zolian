using Darkages.Common;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("WorldShout")]
public class WorldShout(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new (0x0001, "World Announce"),
        };

        client.SendOptionsDialog(Mundane, "What do you wish to announce?", options.ToArray());

        client.DlgSession ??= new DialogSession()
        {
            Callback = OnResponse
        };
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
            return;
        }

        if (!string.IsNullOrEmpty(args))
        {
            foreach (var m in GetObjects<Aisling>(null, n => n.LoggedIn))
                m.Client.SystemMessage($"{{=s{client.Aisling}: {args}");

            client.CloseDialog();
            client.CloseDialog();
        }
        else
        {
            switch (responseID)
            {
                case 0x0001:
                    {
                        client.SendTextInput(Mundane, "What do you want to shout?", "Remember to always be kind and considerate.", 40);
                    }
                    break;
            }
        }
    }
}