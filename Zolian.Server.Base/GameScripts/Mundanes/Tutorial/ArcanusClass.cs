﻿using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("Arcanus Class")]
public class ArcanusClass : MundaneScript
{
    public ArcanusClass(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<Dialog.OptionsDataItem>
            {
                new (0x02, "You look intelligent."),
                new (0x03, "Nothing, I'm sorry for bothering you.")
            };
            client.SendOptionsDialog(Mundane,
                "Are you interested in the Arcanus class?",
                options.ToArray());
        }
        else
        {
            client.SendMessage(0x04, "I'm set in my ways.");
        }
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        if (responseID is > 0x0001 and < 0x0003)
        {
            client.SendOptionsDialog(Mundane, "The path of elemental magic is long and hard, if you wish to master it; Look no further than our class.");
        }
        else
        {
            client.SendOptionsDialog(Mundane, "Alright, speak to me if you wish to know about our class.");
            Task.Delay(2000).ContinueWith(ct => { client.CloseDialog(); });
        }
    }
}