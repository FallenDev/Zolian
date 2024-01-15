using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.NorthPole;

[Script("Santa")]
public class Santa(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.SavedChristmas)
        {
            options.Add(new(0x01, "Merry Christmas"));
            client.SendOptionsDialog(Mundane, "Hooo! Hoo!!! Thank you once again my dear Aisling!", options.ToArray());
            return;
        }
        else
        {
            options.Add(new(0x03, "How can I save Christmas?"));
        }

        if (client.Aisling.HasInInventory("Christmas Spirit", 7))
        {
            options.Add(new(0x04, "Christmas Spirit"));
        }

        client.SendOptionsDialog(Mundane, "Ho! Ho! Ho! Merry Christmas, I'm sorry you've caught me at a bad time.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "Thank you, Saint Nickolas")
                    };

                    var buffDion = new buff_ArdDion();
                    var buffAite = new buff_DiaAite();
                    client.EnqueueBuffAppliedEvent(client.Aisling, buffDion, TimeSpan.FromSeconds(buffDion.Length));
                    client.EnqueueBuffAppliedEvent(client.Aisling, buffAite, TimeSpan.FromSeconds(buffAite.Length));
                    client.SendOptionsDialog(Mundane, "And a Good Tidings to you! Hoo Hoo Hoo!!!", options.ToArray());
                    break;
                }
            case 0x02:
                client.CloseDialog();
                break;
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "I'll take care of them")
                    };

                    client.SendOptionsDialog(Mundane, "Hello little one, this isn't some ordinary cold. Once a year the monsters surrounding Mount Merry drain our Christmas spirit. " +
                                                      "They do so by stealing our gifts and attacking our Elves. I use up all of my magical energy protecting this place. To restore my " +
                                                      "energy, I'll need some Christmas Spirit Essence. Perhaps you can persuade the unruly to give some up without harming them? But! I " +
                                                      "doubt it! Hoo Hooo *cough*", options.ToArray());

                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "Thank you, Saint Nickolas")
                    };

                    var item = client.Aisling.HasItemReturnItem("Christmas Spirit");

                    if (item != null)
                    {
                        client.Aisling.QuestManager.SavedChristmas = true;
                        client.Aisling.Inventory.RemoveRange(client, item, 7);
                        client.GiveItem("Santa's Pileus");
                        client.SendAttributes(StatUpdateType.WeightGold);
                        var buffDion = new buff_ArdDion();
                        var buffAite = new buff_DiaAite();
                        client.EnqueueBuffAppliedEvent(client.Aisling, buffDion, TimeSpan.FromSeconds(buffDion.Length));
                        client.EnqueueBuffAppliedEvent(client.Aisling, buffAite, TimeSpan.FromSeconds(buffAite.Length));

                        var legend = new Legend.LegendItem
                        {
                            Key = "LSanta1",
                            Time = DateTime.UtcNow,
                            Color = LegendColor.RedPurpleG2,
                            Icon = (byte)LegendIcon.Heart,
                            Text = "Restored Santa's Spirit"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Oh, I thought you had some essence. Please hurry.");
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "It is a Merry Christmas indeed! Thank you Aisling, you have saved Christmas.", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}