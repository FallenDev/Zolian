using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.NorthPole;

[Script("Reindeer Help")]
public class Reindeer(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.RescuedReindeer)
        {
            client.SendOptionsDialog(Mundane, "Now we're ready for the big night! Hey Dasher, get back in your pen!", options.ToArray());
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Dasher, Dancer, Prancer, and Vixen..");
            return;
        }
        else
        {
            options.Add(new(0x03, "What happened here?"));
        }

        if (client.Aisling.HasInInventory("Captured Reindeer", 9))
        {
            options.Add(new(0x04, "I've caught them!"));
        }

        client.SendOptionsDialog(Mundane, "Oh no, no, no! Not again! Aisling, I need your help!", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x02:
                client.CloseDialog();
                break;
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "I'll take a look around")
                    };

                    client.SendOptionsDialog(Mundane, "We made their pens out of candy canes, wait! I know, I know; this isn't what you're thinking! " +
                                                      "Reindeer actually hate candy canes, well all except Vixen. Vixen must of helped the others escape. Can " +
                                                      "you check the surrounding areas and bring them back to me? There's a total of {=q9", options.ToArray());

                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "I will")
                    };

                    var item = client.Aisling.HasItemReturnItem("Captured Reindeer");

                    if (item != null)
                    {
                        client.Aisling.QuestManager.RescuedReindeer = true;
                        
                        for (var i = 0; i < 9; i++)
                        {
                            var reindeer = client.Aisling.HasItemReturnItem("Captured Reindeer");
                            client.Aisling.Inventory.RemoveFromInventory(client, reindeer);
                        }

                        client.GiveItem("Santa's Beard");
                        client.SendAttributes(StatUpdateType.WeightGold);

                        var legend = new Legend.LegendItem
                        {
                            Key = "LReindeer1",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.RedPurpleG2,
                            Icon = (byte)LegendIcon.Heart,
                            Text = "Stopped Vixen's Scheme"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Please hurry.");
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "While you were away, I remade the pens! Come back sometime to visit", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}