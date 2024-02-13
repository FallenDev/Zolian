using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Rionnag;

[Script("Isaias")]
public class Isaias(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.HasItem("Illegible Treasure Map") &&
            client.Aisling.ExpLevel >= 150 &&
            !client.Aisling.QuestManager.UnknownStart)
        {
            options.Add(new(0x01, "I found this note on the back of a map"));
            client.SendOptionsDialog(Mundane, "What have ye here?", options.ToArray());
            return;
        }

        if (client.Aisling.ExpLevel >= 190 &&
            !client.Aisling.QuestManager.PirateShipAccess &&
            client.Aisling.QuestManager.UnknownStart)
        {
            options.Add(new(0x03, "I'll head that way then"));
            client.SendOptionsDialog(Mundane, "So you are foolhardy enough to return? I guess I can't talk sense into everyone with a death wish. So be it then... My brother left some items on a ship off the coast of Lynith Beach. Though do be careful. Last I heard it was overrun with all sorts of monsters.", options.ToArray());
            return;
        }

        if (client.Aisling.HasItem("Scuba Schematics") &&
            client.Aisling.HasItem("Hastily Written Notes") &&
            !client.Aisling.QuestManager.ScubaSchematics)
        {
            options.Add(new(0x04, "I'm not sure?"));
            client.SendOptionsDialog(Mundane, "What've ye found there?", options.ToArray());
            return;
        }

        if (client.Aisling.ExpLevel >= 220 &&
            client.Aisling.QuestManager.ScubaSchematics &&
            !client.Aisling.QuestManager.ScubaMaterialsQuest)
        {
            options.Add(new(0x07, "What exactly do we need?"));
            client.SendOptionsDialog(Mundane, "Ahoy there Aisling. I managed to decipher these documents, but bringing my brother's creation to life won't be no easy task. We'll need some unique monster parts and special Ore to get this thing built.", options.ToArray());
            return;
        }

        if (client.Aisling.HasItem("Flawless Ruby") &&
            client.Aisling.HasInInventory("Human Skin", 10) &&
            client.Aisling.HasInInventory("Breath Sack", 2) &&
            client.Aisling.HasInInventory("Rum", 25) &&
            client.Aisling.QuestManager.ScubaMaterialsQuest)
        {
            options.Add(new(0x09, "{=q*Hand over items*"));
            client.SendOptionsDialog(Mundane, "How'd ya manage?", options.ToArray());
            return;
        }

        if (client.Aisling.ExpLevel >= 220 &&
            client.Aisling.QuestManager.ScubaGearCrafted &&
            !client.Aisling.HasItem("Scuba Gear"))
        {
            options.Add(new(0x0C, "Help! I seem to have lost my Scuba Gear"));
            client.SendOptionsDialog(Mundane, "*belches* HAR! No worries, I've made a few extra.", options.ToArray());
            return;
        }

        //continuation of quest-line


        if (client.Aisling.LegendBook.Has("Sea Legs") && !client.Aisling.LegendBook.Has("Sea Worthy"))
        {
            client.SendOptionsDialog(Mundane, "Ah, Sea Legs, I have nothing for you at this moment.", options.ToArray());
            return;
        }

        if (client.Aisling.LegendBook.Has("Sea Worthy"))
        {
            client.SendOptionsDialog(Mundane, "Ah, fellow Pirate how fares the seas?", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "Bugger Off! I've no interest in the affairs of fledgling adventurers!", options.ToArray());
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
                        new (0x02, "My condolences but..")
                    };

                    client.SendOptionsDialog(Mundane, "That damned fool.... He really did end up going to far didn't he.", options.ToArray());
                    break;
                }
            case 0x02:
                {
                    MessageInABottle(client);
                    var options = new List<Dialog.OptionsDataItem>();

                    if (client.Aisling.ExpLevel >= 190)
                    {
                        options.Add(new(0x03, "I'll head that way then"));
                        client.SendOptionsDialog(Mundane, "My brother left some items on a ship off the coast of Lynith Beach. Though do be careful. Last I heard it was overrun with all sorts of monsters.", options.ToArray());
                        return;
                    }

                    options.Add(new(0x00, "We'll see"));
                    client.SendOptionsDialog(Mundane, "Aye, he did leave me some information only a fool like himself would value. Come back when you are stronger. I'll not have a weakling's death on my conscience...", options.ToArray());
                    break;
                }
            case 0x03:
                {
                    client.Aisling.QuestManager.PirateShipAccess = true;
                    client.SendOptionsDialog(Mundane, "Go to the ship off the cost of Lynith, make your way to the Captain's Chambers to retrieve the schematics for the Scuba Gear and any notes you find.");
                    break;
                }
            case 0x04:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "What is it?")
                    };

                    client.SendOptionsDialog(Mundane, "Well I'll be damned. That crazy bastard managed to make that after all!", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x06, "Really, underwater!?")
                    };

                    client.SendOptionsDialog(Mundane, "I hope you aren't afraid to get your hair wet Aisling! This contraption is used to breath under water!", options.ToArray());
                    break;
                }
            case 0x06:
                {
                    GoingSailing(client);
                    var options = new List<Dialog.OptionsDataItem>();

                    if (client.Aisling.ExpLevel >= 220)
                    {
                        options.Add(new(0x00, "Alright, I'll be back"));
                        client.SendOptionsDialog(Mundane, "Har Har! I'd hope that'd bring you to yer senses and you'd give up on this crazy quest, but by now I should know better. I'll need some time to decipher his plans and come up with a way to build it.", options.ToArray());
                        return;
                    }

                    options.Add(new(0x00, "Alright, I'll be back"));
                    client.SendOptionsDialog(Mundane, "Har Har! I'd hope that'd bring you to yer senses and you'd give up on this crazy quest, but by now I should know better. I'll need some time to decipher his plans and come up with a way to build it. It won't be easy, so I suggest getting stronger while you wait.", options.ToArray());
                    break;
                }
            case 0x07:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x08, "Why Rum?")
                    };

                    client.Aisling.QuestManager.ScubaMaterialsQuest = true;
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "Flawless Ruby x1\n" +
                        "Human Skin x10\n" +
                        "Breath Sack x2\n" +
                        "Rum x25");
                    client.SendOptionsDialog(Mundane, "I'm a Pirate! We never do anything fun while sober! Har Har!!", options.ToArray());
                    break;
                }
            case 0x08:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x00, "Alright then")
                    };

                    client.SendOptionsDialog(Mundane, "{=q*continues to laugh in a deep belly grunt*", options.ToArray());
                    break;
                }
            case 0x09:
                {
                    GoingFishing(client);
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x0A, "No, it's right there.")
                    };

                    client.SendOptionsDialog(Mundane, "Where's the Rum? Did ye forget the Rum!?", options.ToArray());
                    break;
                }
            case 0x0A:
                {
                    var item = new Item();
                    item = item.Create(client.Aisling, "Scuba Gear");
                    item.GiveTo(client.Aisling);
                    client.Aisling.QuestManager.ScubaGearCrafted = true;

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0B, "...")
                    };

                    client.SendOptionsDialog(Mundane, "Ah, that's a good lad I say, that's a good lad. Welp, I'll be done in a jiffy.", options.ToArray());
                    break;
                }
            case 0x0B:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x00, "*Take Scuba Gear*")
                    };

                    client.SendOptionsDialog(Mundane, "There ya go. All finished and ready to use!", options.ToArray());
                    break;
                }
            case 0x0C:
                {
                    // Called when Scuba Gear is lost
                    var item = new Item();
                    item = item.Create(client.Aisling, "Scuba Gear");
                    item.GiveTo(client.Aisling);

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x00, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "*hands you some Scuba Gear*", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message)
    {
        if (message.Contains("Arg"))
            client.SendOptionsDialog(Mundane, "Arrrrgggg! What ye know, I'll sing ya a sea Shanty!");
    }

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

    private static void GoingSailing(WorldClient client)
    {
        var exp = Random.Shared.Next(32000000, 44000000); // level 190 * 30 - 35 reward
        var item = client.Aisling.HasItemReturnItem("Scuba Schematics");
        if (item != null)
            client.Aisling.Inventory.RemoveFromInventory(client, item);
        var item2 = client.Aisling.HasItemReturnItem("Hastily Written Notes");
        if (item2 != null)
            client.Aisling.Inventory.RemoveFromInventory(client, item2);

        client.Aisling.QuestManager.ScubaSchematics = true;
        client.GiveExp(exp);
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
        client.SendAttributes(StatUpdateType.WeightGold);

        var legend = new Legend.LegendItem
        {
            Key = "LUnDepths2",
            Time = DateTime.UtcNow,
            Color = LegendColor.GreenG2,
            Icon = (byte)LegendIcon.Community,
            Text = "Sea Legs"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }

    private static void GoingFishing(WorldClient client)
    {
        var exp = Random.Shared.Next(48000000, 63000000); // level 220 * 30 - 35 reward
        var item = client.Aisling.HasItemReturnItem("Flawless Ruby");
        if (item != null)
            client.Aisling.Inventory.RemoveRange(client, item, 1);
        var item2 = client.Aisling.HasItemReturnItem("Human Skin");
        if (item2 != null)
            client.Aisling.Inventory.RemoveRange(client, item2, 10);
        var item3 = client.Aisling.HasItemReturnItem("Breath Sack");
        if (item3 != null)
            client.Aisling.Inventory.RemoveRange(client, item3, 2);
        var item4 = client.Aisling.HasItemReturnItem("Rum");
        if (item4 != null)
            client.Aisling.Inventory.RemoveRange(client, item4, 25);

        client.GiveExp(exp);
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
        client.SendAttributes(StatUpdateType.WeightGold);

        var legend = new Legend.LegendItem
        {
            Key = "LUnDepths3",
            Time = DateTime.UtcNow,
            Color = LegendColor.GreenG2,
            Icon = (byte)LegendIcon.Community,
            Text = "Sea Worthy"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }
}