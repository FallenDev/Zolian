using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("Cleric Class")]
public class ClericClass : MundaneScript
{
    public ClericClass(WorldServer server, Mundane mundane) : base(server, mundane) { }

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
                new (0x02, "Yes"),
                new (0x03, "Nothing, I'm sorry for bothering you.")
            };
            client.SendOptionsDialog(Mundane,
                "Are you interested in becoming a Cleric?",
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
            client.SendOptionsDialog(Mundane, "We turn the undead, are immune to vampirism; Support our allies in battle and can battle almost as good as a Defender. Ignore the other classes, you want to be one of us.");
        }
        else
        {
            client.SendOptionsDialog(Mundane, "Alright, I'll be here.");
            Task.Delay(2000).ContinueWith(ct => { client.CloseDialog(); });
        }
    }
}