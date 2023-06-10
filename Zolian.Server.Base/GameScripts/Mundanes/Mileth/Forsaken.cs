using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Forsaken")]
public class Forsaken : MundaneScript
{
    public Forsaken(GameServer server, Mundane mundane) : base(server, mundane) { }

    private Dictionary<Class, int> HPReqs = new()
    {
        {Class.Berserker, 8500},
        {Class.Defender, 10000},
        {Class.Assassin, 7000},
        {Class.Cleric, 6000},
        {Class.Arcanus, 5500},
        {Class.Monk, 9500}
    };

    private Dictionary<Class, int> MPReqs = new()
    {
        {Class.Berserker, 5500},
        {Class.Defender, 6000},
        {Class.Assassin, 7000},
        {Class.Cleric, 9500},
        {Class.Arcanus, 10000},
        {Class.Monk, 8500}
    };

    private Dictionary<Class, string> ItemsReqs = new()
    {
        {Class.Defender, "Ceannlaidir's Enchanted Sword"},
        {Class.Cleric, "Glioca's Secret"},
        {Class.Assassin, "Fiosachd's Lost Flute"},
        {Class.Berserker, "Ceannlaidir's Tamed Sword"},
        {Class.Arcanus, "Luathas's Lost Relic"},
        {Class.Monk, "Cail's Hourglass"}
    };

    private Dictionary<Class, string> MaxSkillReqs = new()
    {
        {Class.Defender, "Perfect Form"},
        {Class.Cleric, "Disintegrate"},
        {Class.Assassin, "Behind Jester"},
        {Class.Berserker, "Titan's Cleave"},
        {Class.Arcanus, "Tabhair De Eadrom"},
        {Class.Monk, "Mor Dion"}
    };

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>();

        if (client.Aisling.Stage == ClassStage.Class)
        {
            options.Add(new OptionsDataItem(0x02, "Advanced classing"));
            options.Add(new OptionsDataItem(0x03, "Dedication to my class"));
            client.SendOptionsDialog(Mundane, "Hello there young one.", options.ToArray());
        }

        if (client.Aisling.Stage == ClassStage.Dedicated)
        {
            options.Add(new OptionsDataItem(0x01, "Ascending to Master"));
            client.SendOptionsDialog(Mundane, "Hello there devoted one.", options.ToArray());
        }

        if (client.Aisling.Stage == ClassStage.Advance)
        {
            options.Add(new OptionsDataItem(0x01, "Ascending to Master"));
            client.SendOptionsDialog(Mundane, "Advanced one, what is it you wish to learn?", options.ToArray());
        }

        if (client.Aisling.Stage == ClassStage.Master)
        {
            options.Add(new OptionsDataItem(0x04, "Reaching Zenith"));
            client.SendOptionsDialog(Mundane, "Hope you are well, experienced one.", options.ToArray());
        }

        if (client.Aisling.Stage == ClassStage.Forsaken)
        {
            client.SendOptionsDialog(Mundane, "Ah, brother; What can I do for you?", options.ToArray());
        }

        options.Add(new OptionsDataItem(0x05, "Nothing for now"));
    }

    public override async void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            // Master
            case 0x01:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x05, "I am ready"),
                    new (0x05, "No")
                };

                client.SendOptionsDialog(Mundane,
                    "The title of {=bMaster{=a, is something that you should to be proud of. Only disciplined Aislings will pass the requirements needed to proceed. Do you wish to attempt?",
                    options.ToArray());
            }
                break;
            // Advanced
            case 0x02:
            {
                var options = new List<OptionsDataItem>();
                if (client.Aisling.Path != Class.Berserker)
                    options.Add(new OptionsDataItem(0x021, "Berserker"));
                if (client.Aisling.Path != Class.Defender)
                    options.Add(new OptionsDataItem(0x022, "Defender"));
                if (client.Aisling.Path != Class.Assassin)
                    options.Add(new OptionsDataItem(0x023, "Assassin"));
                if (client.Aisling.Path != Class.Cleric)
                    options.Add(new OptionsDataItem(0x024, "Cleric"));
                if (client.Aisling.Path != Class.Arcanus)
                    options.Add(new OptionsDataItem(0x025, "Arcanus"));
                if (client.Aisling.Path != Class.Monk)
                    options.Add(new OptionsDataItem(0x026, "Monk"));

                options.Add(new OptionsDataItem(0x05, "None, right now"));

                client.SendOptionsDialog(Mundane,
                    "If only I advanced my class in my youth. What class are you pondering about?", options.ToArray());
            }
                break;
            // Dedication
            case 0x03:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x030, "I'm devote"),
                    new (0x05, "No")
                };

                client.SendOptionsDialog(Mundane,
                    $"Ah devotee, there is nothing wrong with honing your current abilities and sharpening them to learn anew.",
                    options.ToArray());
            }
                break;
            // Forsaken
            case 0x04:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x05, "I seek power"),
                    new (0x05, "No")
                };

                client.SendOptionsDialog(Mundane,
                    $"To master the class of {{=b{client.Aisling.Path}{{=a,\nI require the item {{=q{ItemsReqs[client.Aisling.Path]}{{=a.\n" +
                    $"You must also have at least {{=q{HPReqs[client.Aisling.Path]} {{=aHealth and {{=q{MPReqs[client.Aisling.Path]} {{=aMana.\n" +
                    $"Finally, You must have mastered this discipline {{=q{MaxSkillReqs[client.Aisling.Path]}{{=a.",
                    options.ToArray());
            }
                break;
            case 0x05:
            {
                client.SendMessage(0x03, "Come back if you need advancement.");
                client.CloseDialog();
            }
                break;
            case 0x011:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x03, "{=bTest of Vitality")
                };

                client.SendOptionsDialog(Mundane,
                    $"To master the class of {{=b{client.Aisling.Path}{{=a,\nI require the item {{=q{ItemsReqs[client.Aisling.Path]}{{=a.\n" +
                    $"You must also have at least {{=q{HPReqs[client.Aisling.Path]} {{=aHealth and {{=q{MPReqs[client.Aisling.Path]} {{=aMana.\n" +
                    $"Finally, You must have mastered this discipline {{=q{MaxSkillReqs[client.Aisling.Path]}{{=a.",
                    options.ToArray());
            }
                break;
            case 0x021:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0210, "{=bI will devote myself to Berserker"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qBerserker Requirements:\n" +
                                         "{=aDexterity:{=q 40\n" +
                                         "{=aConstitution:{=q 30\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q500 {=aHealth\n" +
                                         "{=q300 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things, and you understand that berserkers are very agile? Therefore to make an easy transition I'll ask that you have at least the minimum dexterity and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x022:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0220, "{=bI will devote myself to Defender"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qDefender Requirements:\n" +
                                         "{=aStrength:{=q 50\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q500 {=aHealth\n" +
                                         "{=q300 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things, and you understand that defenders are very strong? Therefore to make an easy transition I'll ask that you have at least the minimum strength and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x023:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0230, "{=bI will devote myself to Assassin"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qAssassin Requirements:\n" +
                                         "{=aDexterity:{=q 40\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q300 {=aHealth\n" +
                                         "{=q600 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things. To make an easy transition to the assassin ranks, I'll ask that you have at least the minimum dexterity and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x024:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0240, "{=bI will devote myself to Cleric"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qCleric Requirements:\n" +
                                         "{=aIntelligence:{=q 40\n" +
                                         "{=aConstitution:{=q 30\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q500 {=aHealth\n" +
                                         "{=q400 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things. You'll need a high aptitude for knowledge and be able to take some hits. I'll ask that you have at least the minimum intelligence, constitution, and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x025:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0250, "{=bI will devote myself to Arcanus"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qArcanus Requirements:\n" +
                                         "{=aIntelligence:{=q 50\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q200 {=aHealth\n" +
                                         "{=q700 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things. Arcanists have divine knowledge of the elements and it'll require you to meet the minimum intelligence requirement and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x026:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0260, "{=bI will devote myself to Monk"),
                    new (0x05, "No")
                };

                client.SendMessage(0x08, "{=qMonk Requirements:\n" +
                                         "{=aConstitution:{=q 40\n" +
                                         "{=aDexterity:{=q 30\n" +
                                         "{=bSacrifice:\n" +
                                         "{=q400 {=aHealth\n" +
                                         "{=q400 {=aMana");

                client.SendOptionsDialog(Mundane, "Let me check a few things. Monks pull power from their chakra and are very fluid in their motions, you'll need at least the minimum constitution, dexterity and a slight sacrifice to your vitality.",
                    options.ToArray());
            }
                break;
            case 0x0210:
            {
                if (client.Aisling._Dex >= 40)
                {
                    if (client.Aisling._Con >= 30)
                    {
                        if (client.Aisling.BaseHp >= 650 && client.Aisling.BaseMp >= 450)
                        {
                            client.Aisling.BaseHp -= 500;
                            client.Aisling.BaseMp -= 300;
                            client.Aisling.PastClass = client.Aisling.Path;
                            client.Aisling.Path = Class.Berserker;
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                            await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                            var legend = new Legend.LegendItem
                            {
                                Category = "Class",
                                Time = DateTime.Now,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Value = "Advanced to the path of Berserker"
                            };
                            Berserker(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendStats(StatusFlags.All);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Berserker");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                    }
                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                }
            }
                break;
            case 0x0220:
            {
                if (client.Aisling._Str >= 50)
                {
                    if (client.Aisling.BaseHp >= 650 && client.Aisling.BaseMp >= 450)
                    {
                        client.Aisling.BaseHp -= 500;
                        client.Aisling.BaseMp -= 300;
                        client.Aisling.PastClass = client.Aisling.Path;
                        client.Aisling.Path = Class.Defender;
                        await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                        await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                        await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                        var legend = new Legend.LegendItem
                        {
                            Category = "Class",
                            Time = DateTime.Now,
                            Color = LegendColor.Red,
                            Icon = (byte)LegendIcon.Victory,
                            Value = "Advanced to the path of Defender"
                        };
                        Defender(client);
                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.Aisling.Stage = ClassStage.Advance;
                        client.SendStats(StatusFlags.All);
                        foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                        {
                            announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Defender");
                        }
                        client.CloseDialog();
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                    }

                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the strength requirement.", options.ToArray());
                }
            }
                break;
            case 0x0230:
            {
                if (client.Aisling._Dex >= 40)
                {
                    if (client.Aisling.BaseHp >= 450 && client.Aisling.BaseMp >= 750)
                    {
                        client.Aisling.BaseHp -= 300;
                        client.Aisling.BaseMp -= 600;
                        client.Aisling.PastClass = client.Aisling.Path;
                        client.Aisling.Path = Class.Assassin;
                        await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                        await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                        await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                        var legend = new Legend.LegendItem
                        {
                            Category = "Class",
                            Time = DateTime.Now,
                            Color = LegendColor.Red,
                            Icon = (byte)LegendIcon.Victory,
                            Value = "Advanced to the path of Assassin"
                        };
                        Assassin(client);
                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.Aisling.Stage = ClassStage.Advance;
                        client.SendStats(StatusFlags.All);
                        foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                        {
                            announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Assassin");
                        }
                        client.CloseDialog();
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                    }

                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                }
            }
                break;
            case 0x0240:
            {
                if (client.Aisling._Int >= 40)
                {
                    if (client.Aisling._Con >= 30)
                    {
                        if (client.Aisling.BaseHp >= 650 && client.Aisling.BaseMp >= 550)
                        {
                            client.Aisling.BaseHp -= 500;
                            client.Aisling.BaseMp -= 400;
                            client.Aisling.PastClass = client.Aisling.Path;
                            client.Aisling.Path = Class.Cleric;
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                            await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                            var legend = new Legend.LegendItem
                            {
                                Category = "Class",
                                Time = DateTime.Now,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Value = "Advanced to the path of Cleric"
                            };
                            Cleric(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendStats(StatusFlags.All);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Cleric");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                    }
                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the intelligence requirement.", options.ToArray());
                }
            }
                break;
            case 0x0250:
            {
                if (client.Aisling._Int >= 50)
                {
                    if (client.Aisling.BaseHp >= 350 && client.Aisling.BaseMp >= 850)
                    {
                        client.Aisling.BaseHp -= 200;
                        client.Aisling.BaseMp -= 700;
                        client.Aisling.PastClass = client.Aisling.Path;
                        client.Aisling.Path = Class.Arcanus;
                        await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                        await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                        await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                        var legend = new Legend.LegendItem
                        {
                            Category = "Class",
                            Time = DateTime.Now,
                            Color = LegendColor.Red,
                            Icon = (byte)LegendIcon.Victory,
                            Value = "Advanced to the path of Arcanus"
                        };
                        Arcanus(client);
                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.Aisling.Stage = ClassStage.Advance;
                        client.SendStats(StatusFlags.All);
                        foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                        {
                            announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Arcanus");
                        }
                        client.CloseDialog();
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                    }

                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the intelligence requirement.", options.ToArray());
                }
            }
                break;
            case 0x0260:
            {
                if (client.Aisling._Con >= 40)
                {
                    if (client.Aisling._Dex >= 30)
                    {
                        if (client.Aisling.BaseHp >= 550 && client.Aisling.BaseMp >= 550)
                        {
                            client.Aisling.BaseHp -= 400;
                            client.Aisling.BaseMp -= 400;
                            client.Aisling.PastClass = client.Aisling.Path;
                            client.Aisling.Path = Class.Monk;
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                            await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                            var legend = new Legend.LegendItem
                            {
                                Category = "Class",
                                Time = DateTime.Now,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Value = "Advanced to the path of Monk"
                            };
                            Monk(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendStats(StatusFlags.All);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Monk");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                    }
                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                    client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                }
            }
                break;
            case 0x030:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x031, "Let's proceed"),
                    new (0x05, "I've changed my mind")
                };
                client.SendMessage(0x08, "{=qDedicated Buffs:\n" +
                                         "{=gBase HP: {=e1000\n" +
                                         "{=gBase MP: {=e1000\n" +
                                         "{=gStr: {=e5\n" +
                                         "{=gInt: {=e5\n" +
                                         "{=gWis: {=e5\n" +
                                         "{=gCon: {=e5\n" +
                                         "{=gDex: {=e5\n" +
                                         "{=gDmg: {=e5\n" +
                                         "{=gReflex: {=e5\n");
                client.SendOptionsDialog(Mundane,
                    "Pledging your loyalty to your current class is a noble gesture. If you're sure we can proceed, I just need to ensure that you're the correct insight first (Level 50).",
                    options.ToArray());
                break;
            }
            case 0x031:
            {
                if (client.Aisling.ExpLevel >= 50)
                {
                    client.Aisling.BaseHp += 1000;
                    client.Aisling.BaseMp += 1000;
                    client.Aisling._Str += 5;
                    client.Aisling._Int += 5;
                    client.Aisling._Wis += 5;
                    client.Aisling._Con += 5;
                    client.Aisling._Dex += 5;
                    client.Aisling._Dmg += 5;
                    client.Aisling._Hit += 5;
                    await Task.Delay(250).ContinueWith(ct => { client.Aisling.Animate(303); });
                    await Task.Delay(250).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                    await Task.Delay(450).ContinueWith(ct => { client.Aisling.Animate(303); });
                    var legend = new Legend.LegendItem
                    {
                        Category = "Class",
                        Time = DateTime.Now,
                        Color = LegendColor.Yellow,
                        Icon = (byte)LegendIcon.Victory,
                        Value = $"Dedication to {client.Aisling.Path}"
                    };
                    client.Aisling.LegendBook.AddLegend(legend, client);
                    client.Aisling.Stage = ClassStage.Dedicated;
                    client.SendStats(StatusFlags.All);
                    foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                    {
                        announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has reaffirmed their dedication to their class");
                    }

                    var options = new List<OptionsDataItem>
                    {
                        new (0x05, "Thank you")
                    };

                    client.SendOptionsDialog(Mundane, "It's a long road to perfecting something. Perform it daily until it is unrecognizable.", options.ToArray());
                }
                break;
            }
        }
    }

    private static void Berserker(IGameClient client)
    {
        Skill.GiveTo(client.Aisling, "Onslaught", 1);
        client.LoadSkillBook();
    }

    private static void Defender(IGameClient client)
    {
        Skill.GiveTo(client.Aisling, "Assault", 1);
        client.LoadSkillBook();
    }

    private static void Assassin(IGameClient client)
    {
        Skill.GiveTo(client.Aisling, "Stab", 1);
        client.LoadSkillBook();
    }

    private static void Monk(IGameClient client)
    {
        Skill.GiveTo(client.Aisling, "Punch", 1);
        client.LoadSkillBook();
    }

    private static void Cleric(IGameClient client)
    {
        Spell.GiveTo(client.Aisling, "Heal Minor Wounds", 1);
        client.LoadSpellBook();
    }

    private static void Arcanus(IGameClient client)
    {
        Spell.GiveTo(client.Aisling, "Beag Athar", 1);
        Spell.GiveTo(client.Aisling, "Beag Creag", 1);
        Spell.GiveTo(client.Aisling, "Beag Sal", 1);
        Spell.GiveTo(client.Aisling, "Beag Srad", 1);
        Spell.GiveTo(client.Aisling, "Beag Dorcha", 1);
        Spell.GiveTo(client.Aisling, "Beag Eadrom", 1);
        client.LoadSpellBook();
    }
}