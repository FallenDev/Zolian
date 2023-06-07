using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("WorldShout")]
public class WorldShout : MundaneScript
{
    public WorldShout(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>
        {
            new (0x0001, "World Announce"),
        };

        client.SendOptionsDialog(Mundane, "What do you wish to announce?", options.ToArray());

        client.DlgSession ??= new DialogSession(client.Aisling, Mundane.Serial)
        {
            Callback = OnResponse
        };
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
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
                    client.Send(new ReactorInputSequence(Mundane, "What do you want to shout?", "Remember to always be kind and considerate.", 40));
                }
                    break;
            }
        }
    }
}