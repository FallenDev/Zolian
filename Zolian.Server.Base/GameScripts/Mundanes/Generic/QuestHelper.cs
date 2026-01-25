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

                    client.SendDialogSequenceMenu(Mundane, "You've completed your quest, would you like to return?", 0x01, 0x02, options.ToArray());
                    break;
                }
            case 2:
                {
                    client.TransitionToMap(400, new Position(7, 7));
                    if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(400, out var questHelper) || questHelper.Length == 0) return;
                    if (!questHelper.TryGetValue<Mundane>(t => t.Name == "Keela", out var mundane) || mundane == null) return;

                    Task.Delay(350).ContinueWith(_ =>
                    {
                        client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: you move swiftly! Is the task complete?");
                    });
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

                    client.SendDialogSequenceMenu(Mundane, "You've completed your quest, would you like to return?", 0x03, 0x04, options.ToArray());
                    break;
                }
            case 4:
                {
                    client.TransitionToMap(301, new Position(7, 7));
                    if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(301, out var questHelper) || questHelper.Length == 0) return;
                    if (!questHelper.TryGetValue<Mundane>(t => t.Name == "Neal", out var mundane) || mundane == null) return;

                    Task.Delay(350).ContinueWith(_ =>
                    {
                        client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Tidings, you're back so soon!");
                    });
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

                    client.SendDialogSequenceMenu(Mundane, "Ahh completed your first quest! Would you like to return?", 0x05, 0x06, options.ToArray());
                    break;
                }
            case 6:
                {
                    client.TransitionToMap(500, new Position(40, 44));
                    if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(500, out var questHelper) || questHelper.Length == 0) return;
                    if (!questHelper.TryGetValue<Mundane>(t => t.Name == "Pete", out var mundane) || mundane == null) return;

                    Task.Delay(350).ContinueWith(_ =>
                    {
                        client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Welcome back, did you take care of those rats?");
                    });
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

                    client.SendDialogSequenceMenu(Mundane, "You've completed your quest, would you like to return?", 0x07, 0x08, options.ToArray());
                    break;
                }
            case 8:
                {
                    client.TransitionToMap(305, new Position(6, 11));
                    if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(305, out var questHelper) || questHelper.Length == 0) return;
                    if (!questHelper.TryGetValue<Mundane>(t => t.Name == "Lau", out var mundane) || mundane == null) return;

                    Task.Delay(350).ContinueWith(_ =>
                    {
                        client.SendPublicMessage(mundane.Serial, PublicMessageType.Normal, $"{mundane.Name}: Ho Ho Ho, completed your quest did ye?");
                    });
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