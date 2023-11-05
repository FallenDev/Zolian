using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Temple of Void")]
public class TempleOfVoid(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;

        if (client.Aisling.Map.ID == 500)
        {
            TopMenu(client);
        }
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "Approach the Temple"),
            new(0x02, "...")
        };

        client.SendOptionsDialog(Mundane, "Do not visit the temple expecting favors.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
            return;
        }

        if (client.Aisling.Map.ID != 500)
        {
            client.Dispose();
            return;
        }

        switch (responseID)
        {
            case 1:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case >= 50:
                            client.TransitionToMap(14757, new Position(17, 58));
                            break;
                        default:
                            client.TransitionToMap(14757, new Position(18, 58));
                            break;
                    }

                    client.SendAnimation(262, null, client.Aisling.Serial);
                    break;
                }
            case 2:
                {
                    client.CloseDialog();
                    break;
                }
        }
    }
}