using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("Berserker Class")]
public class BerserkerClass : MundaneScript
{
    public BerserkerClass(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<OptionsDataItem>
            {
                new (0x02, "You look strong."),
                new (0x03, "Nothing, I'm sorry for bothering you.")
            };
            client.SendOptionsDialog(Mundane,
                "Are you interested in the Berserker class?",
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
            client.SendOptionsDialog(Mundane, "Our class has it's advantages over the 'Defender' class, while we despise holding two-handed weapons, we instead favor dual wielding blades.");
        }
        else
        {
            client.SendOptionsDialog(Mundane, "Alright, speak to me if you wish to know about our class.");
            Task.Delay(2000).ContinueWith(ct => { client.CloseDialog(); });
        }
    }
}