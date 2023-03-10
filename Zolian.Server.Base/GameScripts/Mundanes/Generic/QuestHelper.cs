using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Quest Helper")]
public class QuestHelper : MundaneScript
{
    public QuestHelper(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client) { }

    public override void TopMenu(IGameClient client) => client.CloseDialog();

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        switch (responseID)
        {
            case 1:
            {
                // Keela
                var options = new List<OptionsDataItem>
                {
                    new(0x02, "{=qReturn me"),
                    new(0x0320, "{=bNo, I'll stay")
                };

                client.SendOptionsDialog(Mundane, "You've completed your quest, would you like to return?.", options.ToArray());
                break;
            }
            case 2:
            {
                client.TransitionToMap(400, new Position(7, 7));
                break;
            }
            case 3:
            {
                // Neal
                var options = new List<OptionsDataItem>
                {
                    new(0x04, "{=qReturn me"),
                    new(0x0320, "{=bNo, I'll stay")
                };

                client.SendOptionsDialog(Mundane, "You've completed your quest, would you like to return?.", options.ToArray());
                break;
            }
            case 4:
            {
                client.TransitionToMap(301, new Position(7, 7));
                break;
            }
            case 800:
            {
                client.CloseDialog();
                break;
            }
        }
    }
}