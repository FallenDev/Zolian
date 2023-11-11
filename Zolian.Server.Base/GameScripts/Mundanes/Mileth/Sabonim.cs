using System.Collections.Concurrent;
using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
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

            switch (client.Aisling.QuestManager.BeltDegree)
            {
                case "White":
                    if (client.Aisling.QuestManager.BeltQuest == "Yellow")
                    {
                        options.Add(new(0x22, "I've slain the Goblin"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x02, "Attainment of the Yellow degree"));
                    break;
                case "Yellow":
                    if (client.Aisling.QuestManager.BeltQuest == "Orange")
                    {
                        options.Add(new(0x23, "I've slain the Polyps"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x03, "Attainment of the Orange degree"));
                    break;
                case "Orange":
                    if (client.Aisling.QuestManager.BeltQuest == "Green")
                    {
                        options.Add(new(0x24, "I've slain the Grimloks"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x04, "Attainment of the Green degree"));
                    break;
                case "Green":
                    if (client.Aisling.QuestManager.BeltQuest == "Purple")
                    {
                        options.Add(new(0x25, "I've slain the Marauders"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x05, "Attainment of the Purple degree"));
                    break;
                case "Purple":
                    if (client.Aisling.QuestManager.BeltQuest == "Blue")
                    {
                        options.Add(new(0x26, "I've slain the Wisps"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x06, "Attainment of the Blue degree"));
                    break;
                case "Blue":
                    if (client.Aisling.QuestManager.BeltQuest == "Brown")
                    {
                        options.Add(new(0x27, "I've removed a group of flies"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x07, "Attainment of the Brown degree"));
                    break;
                case "Brown":
                    if (client.Aisling.QuestManager.BeltQuest == "Red")
                    {
                        options.Add(new(0x28, "I've slain a Lich Lord"));
                        client.SendOptionsDialog(Mundane, "You're back, and so soon!", options.ToArray());
                        return;
                    }
                    options.Add(new(0x08, "Attainment of the Red degree"));
                    break;
                case "Red":
                    if (client.Aisling.QuestManager.BeltQuest == "Black")
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
                client.Aisling.QuestManager.BeltQuest = "Yellow";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bGoblin Soldier");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "In Eastern Woodlands, search and kill a Goblin Soldier. Return to me when you've completed your task");
                break;
            case 0x22:
                if (client.Aisling.HasKilled("Goblin Soldier", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Yellow";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Yellow belt!");
                    client.GiveItem("Yellow Belt");
                    var yellowBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Yellow Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Orange";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bPolyp x2");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "In Abel Dungeon, search and kill two Polyps. Return to me when you've completed your task");
                break;
            case 0x23:
                if (client.Aisling.HasKilled("Polyp", 2))
                {
                    client.Aisling.QuestManager.BeltDegree = "Orange";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Orange belt!");
                    client.GiveItem("Orange Belt");
                    var orangeBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Orange Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Green";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bGrimlok Worker x3");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "In Pravat Mines, search and kill three Grimlok Workers. Return to me when you've completed your task");
                break;
            case 0x24:
                if (client.Aisling.HasKilled("Grimlok Worker", 3))
                {
                    client.Aisling.QuestManager.BeltDegree = "Green";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Green belt!");
                    client.GiveItem("Green Belt");
                    var greenBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Green Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Purple";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bMarauder x5");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "Deep in Mileth's Crypt, search and kill five Marauders. Return to me when you've completed your task");
                break;
            case 0x25:
                if (client.Aisling.HasKilled("Marauder", 5))
                {
                    client.Aisling.QuestManager.BeltDegree = "Purple";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Purple belt!");
                    client.GiveItem("Purple Belt");
                    var purpleBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Purple Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Blue";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bWisp x2");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "Travel to either Western or Eastern Woodlands, search and kill two Wisps. Return to me when you've completed your task");
                break;
            case 0x26:
                if (client.Aisling.HasKilled("Wisp", 2))
                {
                    client.Aisling.QuestManager.BeltDegree = "Blue";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Blue belt!");
                    client.GiveItem("Blue Belt");
                    var blueBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Blue Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Brown";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bFlies");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "Don't you just hate flies? Search and kill a group of them. Return to me when you've completed your task");
                break;
            case 0x27:
                if (client.Aisling.HasKilled("Flies", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Brown";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Brown belt!");
                    client.GiveItem("Brown Belt");
                    var brownBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Brown Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Red";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bLich Lord");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "I promise this will be the last run to the crypt I send you on. I need you to kill a Lich Lord. Return to me when you've completed your task");
                break;
            case 0x28:
                if (client.Aisling.HasKilled("Lich Lord", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Red";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Red belt!");
                    client.GiveItem("Red Belt");
                    var redBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Red Belt Attainment"
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
                client.Aisling.QuestManager.BeltQuest = "Black";
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bOld One");
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Note: Kill quests require you remain logged in.");
                client.SendOptionsDialog(Mundane, "Soon I will call you my equal, my brother. Go find and kill an ancient one called -Old One-, return to me when you've completed your task");
                break;
            case 0x29:
                if (client.Aisling.HasKilled("Old One", 1))
                {
                    client.Aisling.QuestManager.BeltDegree = "Black";
                    client.Aisling.QuestManager.BeltQuest = "";
                    client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                    client.SendOptionsDialog(Mundane, "Ah, you did well, very well. Talk to the Sabonim meditating in the woods to learn more.");
                    client.SendAnimation(1, null, client.Aisling.Serial);
                    client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your Black belt!");
                    client.Aisling.SendTargetedClientMethod(Scope.All, c => client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} earned their Black Belt!"));
                    client.GiveItem("Black Belt");
                    var blackBelt = new Legend.LegendItem
                    {
                        Category = "Training",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.White,
                        Icon = (byte)LegendIcon.Monk,
                        Value = "Black Belt Attainment"
                    };

                    client.Aisling.LegendBook.AddLegend(blackBelt, client);
                    return;
                }

                client.SendOptionsDialog(Mundane, "I sense you are lying, try again");
                break;
            case 0x0A:
                client.SendOptionsDialog(Mundane, "Ah brother, my fellow Black Belt. You have attained all this old stream can afford to you. However, if you so seek it. There is a druid outside of our stream that knows teachings beyond our own.");
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
                client.SendAnimation(1, null, client.Aisling.Serial);
                client.SendBodyAnimation(client.Aisling.Serial, BodyAnimation.HandsUp, 40);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've earned your White belt!");
                client.GiveItem("White Belt");
                var item = new Legend.LegendItem
                {
                    Category = "Training",
                    Time = DateTime.UtcNow,
                    Color = LegendColor.White,
                    Icon = (byte)LegendIcon.Monk,
                    Value = "White Belt Attainment"
                };

                client.Aisling.LegendBook.AddLegend(item, client);
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}