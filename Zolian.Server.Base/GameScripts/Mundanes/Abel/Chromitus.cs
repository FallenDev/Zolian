using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Rifting Warden")]
public class Chromitus : MundaneScript
{
    public Chromitus(WorldServer server, Mundane mundane) : base(server, mundane) { }

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
            new(0x01, "500 - 550"),
            new(0x02, "550 - 600"),
            new(0x03, "600 - 650"),
            new(0x04, "650 - 700"),
            new(0x05, "700 - 750"),
            new(0x06, "750 - 800"),
            new(0x07, "800 - 850"),
            new(0x08, "850 - 900"),
            new(0x09, "900 - 950"),
            new(0x0A, "950 - 1000"),
            new(0x00, "1000+")
        };

        client.SendOptionsDialog(Mundane, "You found your way here.. If you're strong enough, I'll allow you entry to complete the trials.\nNow, which rift would you like to enter?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        var rand = Random.Shared.Next(0, 1);

        switch (responseID)
        {
            case 0x01:
                {
                    client.TransitionToMap(801, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x02:
                {
                    client.TransitionToMap(802, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x03:
                {
                    client.TransitionToMap(803, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x04:
                {
                    client.TransitionToMap(804, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x05:
                {
                    client.TransitionToMap(805, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x06:
                {
                    client.TransitionToMap(806, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x07:
                {
                    client.TransitionToMap(807, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x08:
                {
                    client.TransitionToMap(808, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x09:
                {
                    client.TransitionToMap(809, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
            case 0x0A:
                {
                    client.TransitionToMap(810, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message)
    {
        if (!message.Contains("rift", StringComparison.InvariantCultureIgnoreCase)) return;
        TopMenu(client);
    }
}