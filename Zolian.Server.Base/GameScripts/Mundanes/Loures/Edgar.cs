using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Loures;

[Script("Edgar")]
public class Edgar(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.QuestManager.UnhappyEnding)
        {
            case false when client.Aisling.QuestManager.EternalLove:
                options.Add(new(0x01, "I've heard stories, that you fought in the war?"));
                client.SendOptionsDialog(Mundane, "Eh? What do you want?", options.ToArray());
                return;
            case false:
                client.SendOptionsDialog(Mundane, "Move along stranger, I'm just a broken man.", options.ToArray());
                return;
            case true:
                options.Add(new(0x05, "You fought beside him? Where was it again?"));
                client.SendOptionsDialog(Mundane, "Ah, you again.", options.ToArray());
                break;
        }
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                client.CloseDialog();
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "I'd like to ask you about Rouel")
                    };

                    client.SendOptionsDialog(Mundane, "Aye, that I did. And what of it?", options.ToArray());
                    break;
                }
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "*repeats self*")
                    };

                    client.SendOptionsDialog(Mundane, "Wait, wha.. what name did you just say?", options.ToArray());
                    break;
                }
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x04, "*Tell him the Corina's story*")
                    };

                    client.SendOptionsDialog(Mundane, "Argh! I know what you bleeding said, I was just caught off guard... That's a name I never thought I'd" +
                                                      " hear again. Where did you come by it?", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x05, "So you fought beside him?")
                    };

                    client.Aisling.QuestManager.UnhappyEnding = true;
                    client.SendOptionsDialog(Mundane, "Aye, that lass would know him well. Rouel was my brother, and he saved my life during that " +
                                                      "war at the cost of his own. It’s unfair that someone like him would sacrifice themselves for a hopeless sod like " +
                                                      "me, but that’s what made him great.", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    client.SendOptionsDialog(Mundane, "Aye I did, and if she wants an item to remember him by then I know where to find one. Head to the old War " +
                                                      "Grounds to (27, 2). That’s where he saved me.. at the expense of his own life. It’s a moment I'll never forget as it’s " +
                                                      "the biggest regret of my miserable life! The grounds likely still swarm with Goblins, but a capable Aisling like " +
                                                      "yourself should be able to handle it. Now, go. I have a lot to think over.");
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}