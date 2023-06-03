using Darkages.Common;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Temple of Void")]
public class TempleOfVoid : MundaneScript
{
    public TempleOfVoid(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        if (client.Aisling.Map.ID == 500)
        {
            TopMenu(client);
        }
    }

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>
        {
            new(0x01, "Approach the Temple"),
            new(0x02, "...")
        };

        client.SendOptionsDialog(Mundane, "Do not visit the temple expecting favors.", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
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

                client.SendAnimation(262, client.Aisling, client.Aisling);
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