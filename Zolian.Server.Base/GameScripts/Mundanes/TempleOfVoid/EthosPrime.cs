using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.TempleOfVoid;

[Script("EthosPrime")]
public class EthosPrime(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (!client.Aisling.QuestManager.EndedOmegasRein && client.Aisling.ExpLevel >= 400)
            options.Add(new Dialog.OptionsDataItem(0x01, "Who are you?"));

        if (client.Aisling.HasKilled("Draconic Omega", 1) && !client.Aisling.QuestManager.EndedOmegasRein)
            options.Add(new Dialog.OptionsDataItem(0x03, $"{{=bI've slain Draconic Omega"));

        if (client.Aisling.QuestManager.EndedOmegasRein)
        {
            client.SendOptionsDialog(Mundane, "You have helped us tremendously and for that, we are eternally grateful.");
            return;
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level < 400
                ? "I'm sorry, but you're not quite up to the task yet! Grow more, and return to me."
                : "Aisling, we need your help! Draconic Omega will destroy our realm if we do not stop him.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;
        var exp = Random.Shared.Next(350000000, 500000000);

        switch (responseId)
        {
            case 0x00:
                {
                    client.CloseDialog();
                }
                break;
            case 0x01:
                {
                    if (client.Aisling.QuestManager.EndedOmegasRein)
                    {
                        client.SendOptionsDialog(Mundane, "You have defeated Draconic Omega. Thank you");
                        return;
                    }

                    if (client.Aisling.ExpLevel < 400)
                    {
                        client.SendOptionsDialog(Mundane, "I'm sorry, but you're not quite up to the task yet! Grow more, and return to me.");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "In our universe, we are made up from alloys and electrical vibrations. During our creation there was a separation \n" +
                                                      "a spark if you will. That caused an alternating vibration. This cancelled out a large portion of our populations reasoning and \n" +
                                                      "moral compass. Our brethren are suffering, we first made contact with your world when Dennis who associates himself with the \n" +
                                                      "Adventurer's Guild, broke through a vibration wormhole that was opened up. With that connection now binding our worlds, will \n" +
                                                      "you help us?", [new Dialog.OptionsDataItem(0x02, "Yes"), new Dialog.OptionsDataItem(0x00, "No")]);
                }
                break;
            case 0x02:
                {
                    client.SendOptionsDialog(Mundane, "We need you to defeat Draconic Omega. He is the source of our problems. He is the one who has caused the \n" +
                                                      "vibration to be so strong that it has caused a rift in our world. He is located deep within the Shadow Tower. \n" +
                                                      "To access the tower, you will need to eliminate 5 tanks on the last floor of VoidSphere. Be careful, this \n" +
                                                      "tower is not a solo journey. Our brethren have strong hulls and simple weapons will not be enough.");
                }
                break;
            case 0x03:
                {
                    if (client.Aisling.QuestManager.EndedOmegasRein)
                    {
                        client.SendOptionsDialog(Mundane, "You have defeated Draconic Omega. Thank you");
                        return;
                    }

                    if (client.Aisling.ExpLevel < 400)
                    {
                        client.SendOptionsDialog(Mundane, "I'm sorry, but you're not quite up to the task yet! Grow more, and return to me.");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "I'm afraid he will return, as you cannot destroy matter. However, I felt the vibrations shift in our favor. Thank you my friend");
                    client.GiveExp(exp);
                    client.Aisling.QuestManager.EndedOmegasRein = true;
                    var item = new Item();
                    item = item.Create(client.Aisling, "Auto Spark");
                    item.GiveTo(client.Aisling);

                    var legend = new Legend.LegendItem
                    {
                        Key = "LEthos1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.YellowG4,
                        Icon = (byte)LegendIcon.Warrior,
                        Text = "Ended Draconic Omega's Spark"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}