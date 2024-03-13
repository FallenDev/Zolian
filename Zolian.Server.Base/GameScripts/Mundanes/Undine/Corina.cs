using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Undine;

[Script("Corina")]
public class Corina(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.ExpLevel >= 30 &&
            client.Aisling.QuestManager.EternalLoveStarted &&
            !client.Aisling.QuestManager.EternalLove)
        {
            options.Add(new(0x01, "I'm sorry for your loss, I am here to listen"));
            client.SendOptionsDialog(Mundane, "Go away! I shan't be bothered!", options.ToArray());
            return;
        }

        if (client.Aisling.QuestManager.EternalLove &&
            !client.Aisling.QuestManager.UnhappyEnding)
        {
            client.SendOptionsDialog(Mundane, "Have you spoken to your friend yet? I will await your return.");
            return;
        }

        if (client.Aisling.QuestManager.ReadTheFallenNotes && !client.Aisling.QuestManager.HonoringTheFallen)
        {
            options.Add(new(0x06, "I've spoken to Edgar, and I've found this written to you"));
            client.SendOptionsDialog(Mundane, "Ah Aisling, its nice to see you again. I tried to not get my hopes up...but did you find anything?", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, !client.Aisling.QuestManager.EternalLove ? "*weeps*" : "Thank you, I now have some closure", options.ToArray());
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
                        new (0x02, "I insist, please continue")
                    };

                    client.SendOptionsDialog(Mundane, "I'm afraid it's not a pleasant story to tell, but I've hurt for so long.. If you insist I will.", options.ToArray());
                    break;
                }
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "I'm sorry you have to relive this pain")
                    };

                    client.SendOptionsDialog(Mundane, "I lost my betrothed during the last Great Goblin War. He was a Knight in the King's army" +
                                                      " and he lived every moment of his life living up to the Knight's code. He was valiant, handsome, " +
                                                      "and as generous as anyone you've ever seen. He lite up my world like a torch in the night. I was " +
                                                      "afraid for him when the war began, but he assured me that he would return triumphant. Alas, that " +
                                                      "was the first and last promise he ever broke. *Breaks down in tears*", options.ToArray());
                    break;
                }
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x04, "I might be able to help with that")
                    };

                    client.SendOptionsDialog(Mundane, "I'm okay Aisling. I've lived with this pain for a long while, but time doesn't make it any easier " +
                                                      "to accept. Though I do find some comfort in talking about my beloved Rouel. I only wish I had " +
                                                      "something to remember him by. The war was so brutal, that many of those lost were never able to be properly " +
                                                      "commemorated. If only I had one small belonging of his....", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x05, "I know of a knight who fought there")
                    };

                    client.SendOptionsDialog(Mundane, "Please don't get my hopes up. The pain would be too great if you were to fail.", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    client.Aisling.QuestManager.EternalLove = true;
                    client.SendOptionsDialog(Mundane, "Very well, do what you think you can. I shall be waiting with bated breath.");
                    break;
                }
            case 0x06:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x07, "Of course!")
                    };

                    client.SendOptionsDialog(Mundane, "From Rouel!? Let me see.. *begins to read*", options.ToArray());
                    break;
                }
            case 0x07:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x08, "A true hero, I am honored")
                    };

                    client.SendOptionsDialog(Mundane, "To think he had this much on his shoulders and yet he still strode confidently onto that battle field. " +
                                                      "I never expected to learn so much about his final moments and hardships. I am truly thankful for this Aisling!", options.ToArray());
                    break;
                }
            case 0x08:
                {
                    client.Aisling.QuestManager.HonoringTheFallen = true;
                    client.Aisling.QuestManager.MilethReputation++;
                    client.Aisling.QuestManager.UndineReputation++;
                    var bottle = new Item();
                    bottle = bottle.Create(client.Aisling, "Silver Ingot");
                    bottle.GiveTo(client.Aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, client.Aisling.HasItemReturnItem("Letter to Corina"));
                    var legend = new Legend.LegendItem
                    {
                        Key = "LEternal2",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.PinkRedG13,
                        Icon = (byte)LegendIcon.Heart,
                        Text = "Entrusted with a Dying Wish"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x00, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "Please take this as a token of my appreciation. I broke it in a moment of anger, then had a blacksmith meld it to" +
                                                      " an ingot when I learned about my lover's demise. I've no need for it now that I know the truth of it all. I believe I'll" +
                                                      " go and see Edgar. It's been far too long.", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}