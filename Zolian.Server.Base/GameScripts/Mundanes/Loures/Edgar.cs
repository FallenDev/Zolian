using Darkages.Common;
using Darkages.Enums;
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
            case true when !client.Aisling.QuestManager.GivenTarnishedBreastplate:
                options.Add(new(0x05, "You fought beside him? Where was it again?"));
                client.SendOptionsDialog(Mundane, "Ah, you again.", options.ToArray());
                break;
        }

        if (!client.Aisling.QuestManager.GivenTarnishedBreastplate || client.Aisling.QuestManager.ReadTheFallenNotes) return;
        options.Add(new(0x06, "..And this was addressed to you"));
        client.SendOptionsDialog(Mundane, "You've returned, I see you have his armor.", options.ToArray());
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
                                                      "war at the cost of his own. It's unfair that someone like him would sacrifice themselves for a hopeless sod like " +
                                                      "me, but that's what made him great.", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    client.SendOptionsDialog(Mundane, "Aye I did, and if she wants an item to remember him by then I know where to find one. Head to the old War " +
                                                      "Grounds to (27, 2). That's where he saved me.. at the expense of his own life. It's a moment I'll never forget as it's " +
                                                      "the biggest regret of my miserable life! The grounds likely still swarm with Goblins, but a capable Aisling like " +
                                                      "yourself should be able to handle it. Now, go. I have a lot to think over.");
                    break;
                }
            case 0x06:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x07, "..")
                    };

                    client.SendOptionsDialog(Mundane, "*Tears up as he reads the note* To think, That damn fool... If he knew that was going to happen, why didn't he " +
                                                      "just tell us! We could have regrouped and approached the battle differently!", options.ToArray());
                    break;
                }
            case 0x07:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x08, "I will, he also left her a letter")
                    };

                    client.SendOptionsDialog(Mundane, "Can you speak with Corina for me? While I have you here, take this. I have no use for it anymore.", options.ToArray());
                    break;
                }
            case 0x08:
                {
                    client.Aisling.QuestManager.ReadTheFallenNotes = true;
                    client.Aisling.QuestManager.LouresReputation++;
                    var bottle = new Item();
                    bottle = bottle.Create(client.Aisling, "Teardrop Ruby");
                    bottle.GiveTo(client.Aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, client.Aisling.HasItemReturnItem("Letter to Edgar"));
                    var legend = new Legend.LegendItem
                    {
                        Key = "LEternal1",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.PinkRedG13,
                        Icon = (byte)LegendIcon.Warrior,
                        Text = "Mended a Soldier's Heart"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x00, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "Ah, hopefully it heals her as it has me.", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}