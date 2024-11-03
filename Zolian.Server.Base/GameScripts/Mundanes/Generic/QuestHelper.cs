using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Quest Helper")]
public class QuestHelper(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial) { }

    protected override void TopMenu(WorldClient client) => client.CloseDialog();

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        switch (responseID)
        {
            case 1:
                {
                    // Keela
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x02, "{=qReturn me"),
                        new(0x0320, "{=bNo, I'll stay")
                    };

                    client.SendOptionsDialog(Mundane, "You've completed your quest, would you like to return?", options.ToArray());
                    break;
                }
            case 2:
                {
                    client.CloseDialog();
                    client.TransitionToMap(400, new Position(7, 7));
                    break;
                }
            case 3:
                {
                    // Neal
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x04, "{=qReturn me"),
                        new(0x0320, "{=bNo, I'll stay")
                    };

                    client.SendOptionsDialog(Mundane, "You've completed your quest, would you like to return?", options.ToArray());
                    break;
                }
            case 4:
                {
                    client.CloseDialog();
                    client.TransitionToMap(301, new Position(7, 7));
                    break;
                }
            case 5:
                {
                    // Pete
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x06, "{=qReturn me"),
                        new(0x0320, "{=bNo, I'll stay")
                    };

                    client.SendOptionsDialog(Mundane, "Ahh completed your first quest! Would you like to return?", options.ToArray());
                    break;
                }
            case 6:
                {
                    client.CloseDialog();
                    client.TransitionToMap(500, new Position(40, 44));
                    break;
                }
            case 7:
                {
                    // Lau
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x08, "{=qReturn me"),
                        new(0x0320, "{=bNo, I'll stay")
                    };

                    client.SendOptionsDialog(Mundane, "You've completed your quest, would you like to return?", options.ToArray());
                    break;
                }
            case 8:
                {
                    client.CloseDialog();
                    client.TransitionToMap(305, new Position(6, 11));
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