using System.Collections.Concurrent;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Sabonim")]
public class Sabonim(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.Path == Class.Monk || client.Aisling.PastClass == Class.Monk)
        {
            options.Add(new(0x01, "How can I improve?"));

            if (client.Aisling.JobClass != Job.ShaolinMonk)
                options.Add(new(0x99, "I've lost one of my belts."));

            switch (client.Aisling.QuestManager.BeltDegree)
            {
                case "White":
                    if (client.Aisling.QuestManager.BeltQuest == "Yellow" && client.Aisling.ExpLevel >= 20)
                    {
                        options.Add(new(0x22, "I've slain the Goblin"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x02, "Attainment of the Yellow degree"));
                    break;
                case "Yellow":
                    if (client.Aisling.QuestManager.BeltQuest == "Orange" && client.Aisling.ExpLevel >= 40)
                    {
                        options.Add(new(0x23, "I've slain the Polyps"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x03, "Attainment of the Orange degree"));
                    break;
                case "Orange":
                    if (client.Aisling.QuestManager.BeltQuest == "Green" && client.Aisling.ExpLevel >= 60)
                    {
                        options.Add(new(0x24, "I've slain the Grimloks"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x04, "Attainment of the Green degree"));
                    break;
                case "Green":
                    if (client.Aisling.QuestManager.BeltQuest == "Purple" && client.Aisling.ExpLevel >= 80)
                    {
                        options.Add(new(0x25, "I've slain the Marauders"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x05, "Attainment of the Purple degree"));
                    break;
                case "Purple":
                    if (client.Aisling.QuestManager.BeltQuest == "Blue" && client.Aisling.ExpLevel >= 99)
                    {
                        options.Add(new(0x26, "I've slain the Wisps"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x06, "Attainment of the Blue degree"));
                    break;
                case "Blue":
                    if (client.Aisling.QuestManager.BeltQuest == "Brown" && client.Aisling.ExpLevel >= 110)
                    {
                        options.Add(new(0x27, "I've removed a group of flies"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x07, "Attainment of the Brown degree"));
                    break;
                case "Brown":
                    if (client.Aisling.QuestManager.BeltQuest == "Red" && client.Aisling.ExpLevel >= 125)
                    {
                        options.Add(new(0x28, "I've slain a Lich Lord"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x08, "Attainment of the Red degree"));
                    break;
                case "Red":
                    if (client.Aisling.QuestManager.BeltQuest == "Black" && client.Aisling.ExpLevel >= 200)
                    {
                        options.Add(new(0x29, "I've slain the Old One"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x09, "Attainment of the Black degree"));
                    break;
                case "Black":
                    options.Add(new(0x0A, "Beyond our degrees"));
                    break;
                case "":
                    options.Add(new(0x0B, "Attain my first degree"));
                    break;
            }
        }

        client.SendOptionsDialog(Mundane, "Breathe softly, the spirits can guide you", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        switch (responseID)
        {
            // Starting info
            case 0x01:
                client.SendOptionsDialog(Mundane, "By attaining degrees, you will move through each degree until you've reached Black Belt.");
                break;
            // Belt attainments
            case 0x02:
                if (client.Aisling.ExpLevel >= 20)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (5, 4)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x12:
                if (!args.Equals("Yellow")) return;
                if (client.Aisling.ExpLevel < 20)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Yellow";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bGoblin Soldier");
                client.SendOptionsDialog(Mundane, "In Eastern Woodlands, search and kill a Goblin Soldier. Return to me when you've completed your task");
                break;
            case 0x22:
                if (client.Aisling.HasKilled("Goblin Soldier", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Yellow";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Yellow belt!");
                    client.GiveItem("Yellow Belt");
                    var yellowBelt = new Legend.LegendItem
                    {
                        Key = "YBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Yellow Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(yellowBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x03:
                if (client.Aisling.ExpLevel >= 40)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (13, 9)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x13:
                if (!args.Equals("Orange")) return;
                if (client.Aisling.ExpLevel < 40)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Orange";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bPolyp x2");
                client.SendOptionsDialog(Mundane, "In Abel Dungeon, search and kill two Polyps. Return to me when you've completed your task");
                break;
            case 0x23:
                if (client.Aisling.HasKilled("Polyp", 2))
                {
                    client.Aisling.QuestManager.BeltDegree = "Orange";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Orange belt!");
                    client.GiveItem("Orange Belt");
                    var orangeBelt = new Legend.LegendItem
                    {
                        Key = "OBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Orange Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(orangeBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x04:
                if (client.Aisling.ExpLevel >= 60)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (4, 11)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x14:
                if (!args.Equals("Green")) return;
                if (client.Aisling.ExpLevel < 60)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Green";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bGrimlok Worker x3");
                client.SendOptionsDialog(Mundane, "In Pravat Mines, search and kill three Grimlok Workers. Return to me when you've completed your task");
                break;
            case 0x24:
                if (client.Aisling.HasKilled("Grimlok Worker", 3))
                {
                    client.Aisling.QuestManager.BeltDegree = "Green";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Green belt!");
                    client.GiveItem("Green Belt");
                    var greenBelt = new Legend.LegendItem
                    {
                        Key = "GBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Green Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(greenBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x05:
                if (client.Aisling.ExpLevel >= 80)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (9, 3)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x15:
                if (!args.Equals("Purple")) return;
                if (client.Aisling.ExpLevel < 80)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Purple";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bMarauder x5");
                client.SendOptionsDialog(Mundane, "Deep in Mileth's Crypt, search and kill five Marauders. Return to me when you've completed your task");
                break;
            case 0x25:
                if (client.Aisling.HasKilled("Marauder", 5))
                {
                    client.Aisling.QuestManager.BeltDegree = "Purple";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Purple belt!");
                    client.GiveItem("Purple Belt");
                    var purpleBelt = new Legend.LegendItem
                    {
                        Key = "PBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Purple Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(purpleBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x06:
                if (client.Aisling.ExpLevel >= 99)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (11, 12)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x16:
                if (!args.Equals("Blue")) return;
                if (client.Aisling.ExpLevel < 99)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Blue";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bWisp x2");
                client.SendOptionsDialog(Mundane, "Travel to either Western or Eastern Woodlands, search and kill two Wisps. Return to me when you've completed your task");
                break;
            case 0x26:
                if (client.Aisling.HasKilled("Wisp", 2))
                {
                    client.Aisling.QuestManager.BeltDegree = "Blue";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Blue belt!");
                    client.GiveItem("Blue Belt");
                    var blueBelt = new Legend.LegendItem
                    {
                        Key = "BluBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Blue Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(blueBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x07:
                if (client.Aisling.ExpLevel >= 110)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (3, 7)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x17:
                if (!args.Equals("Brown")) return;
                if (client.Aisling.ExpLevel < 110)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Brown";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bFlies");
                client.SendOptionsDialog(Mundane, "Don't you just hate flies? Search and kill a group of them. Return to me when you've completed your task");
                break;
            case 0x27:
                if (client.Aisling.HasKilled("Flies", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Brown";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Brown belt!");
                    client.GiveItem("Brown Belt");
                    var brownBelt = new Legend.LegendItem
                    {
                        Key = "BroTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Brown Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(brownBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x08:
                if (client.Aisling.ExpLevel >= 125)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (12, 5)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x18:
                if (!args.Equals("Red")) return;
                if (client.Aisling.ExpLevel < 125)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Red";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bLich Lord");
                client.SendOptionsDialog(Mundane, "I promise this will be the last run to the crypt I send you on. I need you to kill a Lich Lord. Return to me when you've completed your task");
                break;
            case 0x28:
                if (client.Aisling.HasKilled("Lich Lord", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Red";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Red belt!");
                    client.GiveItem("Red Belt");
                    var redBelt = new Legend.LegendItem
                    {
                        Key = "RBTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Red Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(redBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x09:
                if (client.Aisling.ExpLevel >= 200)
                {
                    client.SendOptionsDialog(Mundane, "Step to the meditation spot in front of me to begin (7, 13)");
                    return;
                }

                client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                break;
            case 0x19:
                if (!args.Equals("Black")) return;
                if (client.Aisling.ExpLevel < 200)
                {
                    client.SendOptionsDialog(Mundane, "You are not ready for this attainment yet.");
                    return;
                }
                client.Aisling.QuestManager.BeltQuest = "Black";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bOld One");
                client.SendOptionsDialog(Mundane, "Soon I will call you my equal, my brother. Go find and kill an ancient one called -Old One-, return to me when you've completed your task");
                break;
            case 0x29:
                if (client.Aisling.HasKilled("Old One", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Black";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Black belt!");
                    client.Aisling.SendTargetedClientMethod(PlayerScope.All, c => client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} earned their Black Belt!"));
                    client.GiveItem("Black Belt");
                    var blackBelt = new Legend.LegendItem
                    {
                        Key = "BlaTraining",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Text = "Black Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(blackBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x0A:
                client.SendOptionsDialog(Mundane, "Ah brother, my fellow Black Belt. You have attained all this old stream can afford to you. However, if you so seek it. There is a druid outside of our stream that knows teachings beyond our own. The way of the Shaolin Monk.");
                break;
            case 0x0B:
                var optionsB = new List<Dialog.OptionsDataItem>
                {
                    new(0x1B, "Yes, Sabonim")
                };

                client.SendOptionsDialog(Mundane, "Let's meditate together, would you like to learn?", optionsB.ToArray());
                break;
            case 0x1B:
                client.Aisling.QuestManager.BeltDegree = "White";
                client.SendOptionsDialog(Mundane, "Good, there is a lot to learn in breathing exercises alone.");
                client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your White belt!");
                client.GiveItem("White Belt");
                var item = new Legend.LegendItem
                {
                    Key = "WBTraining",
                    IsPublic = true,
                    Time = DateTime.UtcNow,
                    Color = LegendColor.White,
                    Icon = (byte)LegendIcon.Monk,
                    Text = "White Belt Attainment"
                };

                client.Aisling.LegendBook.AddLegend(item, client);
                break;
            case 0x99:
                {
                    ReissueBeltDegree(client);
                    break;
                }
            case 0x991:
                {
                    if (client.Aisling.LegendBook.Has("Yellow Belt Attainment"))
                        client.GiveItem("Yellow Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x992:
                {
                    if (client.Aisling.LegendBook.Has("Orange Belt Attainment"))
                        client.GiveItem("Orange Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x993:
                {
                    if (client.Aisling.LegendBook.Has("Green Belt Attainment"))
                        client.GiveItem("Green Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x994:
                {
                    if (client.Aisling.LegendBook.Has("Purple Belt Attainment"))
                        client.GiveItem("Purple Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x995:
                {
                    if (client.Aisling.LegendBook.Has("Blue Belt Attainment"))
                        client.GiveItem("Blue Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x996:
                {
                    if (client.Aisling.LegendBook.Has("Brown Belt Attainment"))
                        client.GiveItem("Brown Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x997:
                {
                    if (client.Aisling.LegendBook.Has("Red Belt Attainment"))
                        client.GiveItem("Red Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x998:
                {
                    if (client.Aisling.LegendBook.Has("Black Belt Attainment"))
                        client.GiveItem("Black Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
            case 0x999:
                {
                    if (client.Aisling.LegendBook.Has("White Belt Attainment"))
                        client.GiveItem("White Belt");
                    client.SendOptionsDialog(Mundane, "I happened to find this one near a beggar, is it yours?");
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private void ReissueBeltDegree(WorldClient client)
    {
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.LegendBook.Has("White Belt Attainment"))
            options.Add(new(0x999, "White Belt"));
        if (client.Aisling.LegendBook.Has("Yellow Belt Attainment"))
            options.Add(new(0x991, "Yellow Belt"));
        if (client.Aisling.LegendBook.Has("Orange Belt Attainment"))
            options.Add(new(0x992, "Orange Belt"));
        if (client.Aisling.LegendBook.Has("Green Belt Attainment"))
            options.Add(new(0x993, "Green Belt"));
        if (client.Aisling.LegendBook.Has("Purple Belt Attainment"))
            options.Add(new(0x994, "Purple Belt"));
        if (client.Aisling.LegendBook.Has("Blue Belt Attainment"))
            options.Add(new(0x995, "Blue Belt"));
        if (client.Aisling.LegendBook.Has("Brown Belt Attainment"))
            options.Add(new(0x996, "Brown Belt"));
        if (client.Aisling.LegendBook.Has("Red Belt Attainment"))
            options.Add(new(0x997, "Red Belt"));
        if (client.Aisling.LegendBook.Has("Black Belt Attainment"))
            options.Add(new(0x998, "Black Belt"));

        client.SendOptionsDialog(Mundane, "I understand, which belt are you missing?", options.ToArray());
    }
}