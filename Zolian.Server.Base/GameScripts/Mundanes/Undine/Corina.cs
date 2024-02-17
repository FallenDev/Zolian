using Chaos.Common.Definitions;

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
                                                      "to accept. Though I do find some comfort in talking about my beloved <insert name here>. I only wish I had " +
                                                      "something to remember him by. The war was so brutal, that many of those lost were never able to be properly " +
                                                      "commemorated. If only I had one small belonging of his....", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x04, "I know of a knight who fought there")
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
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private static void MessageInABottle(WorldClient client)
    {
        var exp = Random.Shared.Next(29430000, 34335000); // level 160 * 30 - 35 reward
        var item = client.Aisling.HasItemReturnItem("Illegible Treasure Map");
        if (item != null)
            client.Aisling.Inventory.RemoveFromInventory(client, item);

        client.Aisling.QuestManager.UnknownStart = true;
        client.GiveExp(exp);
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
        client.SendAttributes(StatUpdateType.WeightGold);

        var legend = new Legend.LegendItem
        {
            Key = "LUnDepths1",
            Time = DateTime.UtcNow,
            Color = LegendColor.GreenG2,
            Icon = (byte)LegendIcon.Community,
            Text = "Fledgling Treasure Hunter"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }
}