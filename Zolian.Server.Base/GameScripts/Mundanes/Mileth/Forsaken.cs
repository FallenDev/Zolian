using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Forsaken")]
public class Forsaken : MundaneScript
{
    private readonly SortedDictionary<Class, (int str, int intel, int wis, int con, int dex, int hp, int mp)> _advancedClassing = new()
    {
        { Class.Berserker, (5, 5, 5, 30, 40, 628, 428) },
        { Class.Defender, (50, 5, 5, 5, 5, 628, 428) },
        { Class.Assassin, (5, 5, 5, 5, 40, 428, 728) },
        { Class.Cleric, (5, 40, 5, 30, 5, 628, 528) },
        { Class.Arcanus, (5, 50, 5, 5, 5, 328, 828) },
        { Class.Monk, (5, 5, 5, 40, 30, 528, 528) }
    };

    private readonly SortedDictionary<Class, (int hp, int mp, string classItem, string abilityReq)> _mastering = new()
    {
        { Class.Berserker, (7500, 5500, "Ceannlaidir's Tamed Sword", "Titan's Cleave") },
        { Class.Defender, (10000, 5000, "Ceannlaidir's Enchanted Sword", "Perfect Defense") },
        { Class.Assassin, (7000, 6000, "Fiosachd's Lost Flute", "Hiraishin") },
        { Class.Cleric, (8000, 5750, "Glioca's Secret", "Disintegrate") },
        { Class.Arcanus, (5500, 9000, "Luathas's Lost Relic", "Tabhair De Eadrom") },
        { Class.Monk, (8000, 8000, "Cail's Hourglass", "Mor Dion") }
    };

    private readonly SortedDictionary<Class, (int str, int intel, int wis, int con, int dex, string abilityReq)> _forsaking = new()
    {
        { Class.Berserker, (500, 500, 500, 500, 500, "") },
        { Class.Defender, (500, 500, 500, 500, 500, "") },
        { Class.Assassin, (500, 500, 500, 500, 500, "") },
        { Class.Cleric, (500, 500, 500, 500, 500, "") },
        { Class.Arcanus, (500, 500, 500, 500, 500, "") },
        { Class.Monk, (500, 500, 500, 500, 500, "") }
    };

    public Forsaken(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.Stage)
        {
            case ClassStage.Class:
                options.Add(new Dialog.OptionsDataItem(0x02, "Advanced classing"));
                options.Add(new Dialog.OptionsDataItem(0x03, "Dedication to my class"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Hello there young one.", options.ToArray());
                break;
            case ClassStage.Dedicated:
                options.Add(new Dialog.OptionsDataItem(0x01, "Ascending to Master"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Hello there devoted one.", options.ToArray());
                break;
            case ClassStage.Advance:
                options.Add(new Dialog.OptionsDataItem(0x01, "Ascending to Master"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Advanced one, what is it you wish to learn?", options.ToArray());
                break;
            case ClassStage.Master:
                options.Add(new Dialog.OptionsDataItem(0x04, "Reaching Zenith"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Hope you are well, experienced one.", options.ToArray());
                break;
            case ClassStage.Forsaken:
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Ah, brother; What can I do for you?", options.ToArray());
                break;
            case ClassStage.Quest:
                break;
            case ClassStage.MasterLearn:
                break;
            case ClassStage.DedicatedLearn:
                break;
            case ClassStage.AdvanceLearn:
                break;
            case ClassStage.ForsakenLearn:
                break;
        }
    }

    public override async void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            // Master
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x011, "I am ready"),
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
                    var options = new List<Dialog.OptionsDataItem>();
                    if (client.Aisling.Path != Class.Berserker)
                        options.Add(new Dialog.OptionsDataItem(0x021, "Berserker"));
                    if (client.Aisling.Path != Class.Defender)
                        options.Add(new Dialog.OptionsDataItem(0x022, "Defender"));
                    if (client.Aisling.Path != Class.Assassin)
                        options.Add(new Dialog.OptionsDataItem(0x023, "Assassin"));
                    if (client.Aisling.Path != Class.Cleric)
                        options.Add(new Dialog.OptionsDataItem(0x024, "Cleric"));
                    if (client.Aisling.Path != Class.Arcanus)
                        options.Add(new Dialog.OptionsDataItem(0x025, "Arcanus"));
                    if (client.Aisling.Path != Class.Monk)
                        options.Add(new Dialog.OptionsDataItem(0x026, "Monk"));

                    options.Add(new Dialog.OptionsDataItem(0x05, "None, right now"));

                    client.SendOptionsDialog(Mundane,
                        "If only I advanced my class in my youth. What class are you pondering about?", options.ToArray());
                }
                break;
            // Dedication
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
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
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x05, "I seek power"),
                    new (0x05, "No")
                };

                    var (str, intel, wis, con, dex, abilityReq) = _forsaking.First(x => client.Aisling.Path <= x.Key).Value;

                    client.SendOptionsDialog(Mundane,
                        $"The forsaking happens when one reaches their Zenith. After such an event happens, you move as if you're a tier above others",
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
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x05, "{=bI'm sorry not now")
                };

                    var (hp, mp, classItem, abilityReq) = _mastering.First(x => client.Aisling.Path <= x.Key).Value;

                    client.SendOptionsDialog(Mundane,
                        $"To master the class of {{=b{client.Aisling.Path}{{=a,\nI require the item {{=q{classItem}{{=a.\n" +
                        $"You must also have at least {{=q{hp} {{=aHealth and {{=q{mp} {{=aMana.\n" +
                        $"Finally, You must have mastered this discipline {{=q{abilityReq}{{=a.",
                        options.ToArray());
                }
                break;
            case 0x021:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0210, "{=bI will devote myself to Berserker"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0220, "{=bI will devote myself to Defender"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0230, "{=bI will devote myself to Assassin"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0240, "{=bI will devote myself to Cleric"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0250, "{=bI will devote myself to Arcanus"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0260, "{=bI will devote myself to Monk"),
                        new (0x05, "No")
                    };

                    client.SendMessage(0x03, "You cannot go below 128 Health / Mana, plan accordingly");
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
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Berserker <= x.Key).Value;

                    if (client.Aisling._Dex >= dex)
                    {
                        if (client.Aisling._Con >= con)
                        {
                            if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                                {
                                    announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Berserker");
                                }
                                client.CloseDialog();
                            }
                            else
                            {
                                var options = new List<Dialog.OptionsDataItem>
                                {
                                    new (0x05, "Alright")
                                };

                                client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                            }
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                    }
                }
                break;
            case 0x0220:
                {
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Defender <= x.Key).Value;

                    if (client.Aisling._Str >= str)
                    {
                        if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Defender");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }

                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the strength requirement.", options.ToArray());
                    }
                }
                break;
            case 0x0230:
                {
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Assassin <= x.Key).Value;

                    if (client.Aisling._Dex >= dex)
                    {
                        if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Assassin");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }

                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                    }
                }
                break;
            case 0x0240:
                {
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Cleric <= x.Key).Value;

                    if (client.Aisling._Int >= intel)
                    {
                        if (client.Aisling._Con >= con)
                        {
                            if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                                {
                                    announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Cleric");
                                }
                                client.CloseDialog();
                            }
                            else
                            {
                                var options = new List<Dialog.OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                                client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                            }
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the intelligence requirement.", options.ToArray());
                    }
                }
                break;
            case 0x0250:
                {
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Arcanus <= x.Key).Value;

                    if (client.Aisling._Int >= intel)
                    {
                        if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                            {
                                announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Arcanus");
                            }
                            client.CloseDialog();
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                        }

                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the intelligence requirement.", options.ToArray());
                    }
                }
                break;
            case 0x0260:
                {
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Monk<= x.Key).Value;

                    if (client.Aisling._Con >= con)
                    {
                        if (client.Aisling._Dex >= dex)
                        {
                            if (client.Aisling.BaseHp >= hp && client.Aisling.BaseMp >= mp)
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
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                                {
                                    announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has advanced to Monk");
                                }
                                client.CloseDialog();
                            }
                            else
                            {
                                var options = new List<Dialog.OptionsDataItem>
                            {
                                new (0x05, "Alright")
                            };

                                client.SendOptionsDialog(Mundane, "You currently do not meet the vitality requirement.", options.ToArray());
                            }
                        }
                        else
                        {
                            var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Alright")
                        };

                            client.SendOptionsDialog(Mundane, "You currently do not meet the dexterity requirement.", options.ToArray());
                        }
                    }
                    else
                    {
                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Alright")
                    };

                        client.SendOptionsDialog(Mundane, "You currently do not meet the constitution requirement.", options.ToArray());
                    }
                }
                break;
            case 0x030:
                {
                    var options = new List<Dialog.OptionsDataItem>
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
                        client.Aisling.PastClass = client.Aisling.Path;
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
                        client.SendAttributes(StatUpdateType.Full);
                        foreach (var announceClient in ServerSetup.Instance.Game.Clients.Values.Where(x => x.Aisling != null))
                        {
                            announceClient.SendMessage(0x0B, $"{{=c{client.Aisling.Username} has reaffirmed their dedication to their class");
                        }

                        var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x05, "Thank you")
                    };

                        client.SendOptionsDialog(Mundane, "It's a long road to perfecting something. Perform it daily until it is unrecognizable.", options.ToArray());
                    }
                    break;
                }
        }
    }

    private static void Berserker(WorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Onslaught", 1);
        client.LoadSkillBook();
    }

    private static void Defender(WorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Assault", 1);
        client.LoadSkillBook();
    }

    private static void Assassin(WorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Stab", 1);
        client.LoadSkillBook();
    }

    private static void Monk(WorldClient client)
    {
        Skill.GiveTo(client.Aisling, "Punch", 1);
        client.LoadSkillBook();
    }

    private static void Cleric(WorldClient client)
    {
        Spell.GiveTo(client.Aisling, "Heal Minor Wounds", 1);
        client.LoadSpellBook();
    }

    private static void Arcanus(WorldClient client)
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