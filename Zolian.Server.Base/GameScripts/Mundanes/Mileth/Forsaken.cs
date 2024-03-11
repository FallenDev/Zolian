﻿using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Forsaken")]
public class Forsaken(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

    private readonly SortedDictionary<Class, (int hp, int mp, string classItem)> _mastering = new()
    {
        { Class.Berserker, (7500, 5500, "Ceannlaidir's Tamed Sword") },
        { Class.Defender, (10000, 5000, "Ceannlaidir's Enchanted Sword") },
        { Class.Assassin, (7000, 6000, "Fiosachd's Lost Flute") },
        { Class.Cleric, (8000, 5750, "Glioca's Secret") },
        { Class.Arcanus, (5500, 9000, "Luathas's Lost Relic") },
        { Class.Monk, (8000, 8000, "Cail's Hourglass") }
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
            case ClassStage.Dedicated:
            case ClassStage.Class:
                options.Add(new Dialog.OptionsDataItem(0x02, "Advancing"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Hello there young one.", options.ToArray());
                break;
            case ClassStage.Advance:
                options.Add(new Dialog.OptionsDataItem(0x01, "Ascending to Master"));
                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Advanced one, what is it you wish to learn?", options.ToArray());
                break;
            case ClassStage.Master:
                options.Add(client.Aisling.ExpLevel >= 400
                    ? new Dialog.OptionsDataItem(0x03, "Stat Reallocation")
                    : new Dialog.OptionsDataItem(0x06, "Attempt Stat Reallocation"));

                options.Add(new Dialog.OptionsDataItem(0x05, "Nothing for now"));
                client.SendOptionsDialog(Mundane, "Hope you are well, experienced one.", options.ToArray());
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
            // Reallocation
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x04, "Let's proceed"),
                        new (0x05, "No")
                    };

                    client.SendOptionsDialog(Mundane, $"It will cost you; 5,000 Health and 5,000 Mana to proceed.\n{{=bYou will need a base of 5,128 hp/mp", options.ToArray());
                }
                break;
            case 0x04:
                {
                    if (client.Aisling.BaseHp >= 5128 && client.Aisling.BaseMp >= 5128)
                    {
                        var levelAbove250 = client.Aisling.ExpLevel >= 250;
                        client.Aisling.BaseHp -= 5000;
                        client.Aisling.BaseMp -= 5000;
                        client.Aisling._Str = 5;
                        client.Aisling._Int = 5;
                        client.Aisling._Wis = 5;
                        client.Aisling._Con = 5;
                        client.Aisling._Dex = 5;

                        if (levelAbove250)
                        {
                            var points = 500 + (client.Aisling.ExpLevel - 250);
                            points += client.Aisling.AbpLevel;
                            client.Aisling.StatPoints += (short)points;
                        }
                        else
                        {
                            client.Aisling.StatPoints += (short)(client.Aisling.ExpLevel * 2);
                        }

                        await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                        await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                        await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });

                        var legend = new Legend.LegendItem
                        {
                            Key = "Reallocation",
                            Time = DateTime.UtcNow,
                            Color = LegendColor.Red,
                            Icon = (byte)LegendIcon.Victory,
                            Text = "Refocused their Chi"
                        };
                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.SendAttributes(StatUpdateType.Full);

                        foreach (var player in ServerSetup.Instance.Game.Aislings)
                        {
                            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has realigned their chi");
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
                break;
            case 0x05:
                {
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Come back if you need advancement.");
                    client.CloseDialog();
                }
                break;
            case 0x06:
                {
                    client.SendOptionsDialog(Mundane, "I honestly love the enthusiasm, but if I were to cut your soul now. It would only damage it. Come back when you're around level 400.");
                }
                break;
            case 0x011:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x032, "Attempt Mastering"),
                        new (0x05, "{=bI'm sorry not now")
                    };

                    var (hp, mp, classItem) = _mastering.First(x => client.Aisling.Path <= x.Key).Value;

                    client.SendOptionsDialog(Mundane,
                        $"To master the class of {{=b{client.Aisling.Path}{{=a,\nI require the item {{=q{classItem}{{=a.\n" +
                        $"You must also have at least {{=q{hp} {{=aHealth and {{=q{mp} {{=aMana.\n",
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qBerserker Requirements:\n" +
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qDefender Requirements:\n" +
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qAssassin Requirements:\n" +
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qCleric Requirements:\n" +
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qArcanus Requirements:\n" +
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

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot go below 128 Health / Mana, plan accordingly");
                    client.SendServerMessage(ServerMessageType.NonScrollWindow, "{=qMonk Requirements:\n" +
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
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                                await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                var legend = new Legend.LegendItem
                                {
                                    Key = "Class",
                                    Time = DateTime.UtcNow,
                                    Color = LegendColor.Red,
                                    Icon = (byte)LegendIcon.Victory,
                                    Text = "Advanced to the path of Berserker"
                                };
                                Berserker(client);
                                client.Aisling.LegendBook.AddLegend(legend, client);
                                client.Aisling.Stage = ClassStage.Advance;
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var player in ServerSetup.Instance.Game.Aislings)
                                {
                                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Berserker");
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
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            var legend = new Legend.LegendItem
                            {
                                Key = "Class",
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Text = "Advanced to the path of Defender"
                            };
                            Defender(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                            {
                                announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Defender");
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
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            var legend = new Legend.LegendItem
                            {
                                Key = "Class",
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Text = "Advanced to the path of Assassin"
                            };
                            Assassin(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                            {
                                announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Assassin");
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
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                                await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                var legend = new Legend.LegendItem
                                {
                                    Key = "Class",
                                    Time = DateTime.UtcNow,
                                    Color = LegendColor.Red,
                                    Icon = (byte)LegendIcon.Victory,
                                    Text = "Advanced to the path of Cleric"
                                };
                                Cleric(client);
                                client.Aisling.LegendBook.AddLegend(legend, client);
                                client.Aisling.Stage = ClassStage.Advance;
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                                {
                                    announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Cleric");
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
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                            await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                            var legend = new Legend.LegendItem
                            {
                                Key = "Class",
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Red,
                                Icon = (byte)LegendIcon.Victory,
                                Text = "Advanced to the path of Arcanus"
                            };
                            Arcanus(client);
                            client.Aisling.LegendBook.AddLegend(legend, client);
                            client.Aisling.Stage = ClassStage.Advance;
                            client.SendAttributes(StatUpdateType.Full);
                            foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                            {
                                announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Arcanus");
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
                    var (str, intel, wis, con, dex, hp, mp) = _advancedClassing.First(x => Class.Monk <= x.Key).Value;

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
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                await Task.Delay(250).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(97, false)); });
                                await Task.Delay(450).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(303, null, client.Aisling.Serial)); });
                                var legend = new Legend.LegendItem
                                {
                                    Key = "Class",
                                    Time = DateTime.UtcNow,
                                    Color = LegendColor.Red,
                                    Icon = (byte)LegendIcon.Victory,
                                    Text = "Advanced to the path of Monk"
                                };
                                Monk(client);
                                client.Aisling.LegendBook.AddLegend(legend, client);
                                client.Aisling.Stage = ClassStage.Advance;
                                client.SendAttributes(StatUpdateType.Full);
                                foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                                {
                                    announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{client.Aisling.Username} has advanced to Monk");
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
            case 0x032:
                if (client.Aisling.ExpLevel >= 120)
                {
                    var (hp, mp, classItem) = _mastering.First(x => client.Aisling.Path <= x.Key).Value;
                    if (client.Aisling.HasItem(classItem))
                    {
                        if (client.Aisling.BaseHp < hp)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You do not have the required base health");
                            client.CloseDialog();
                            return;
                        }
                        if (client.Aisling.BaseMp < mp)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You do not have the required base mana");
                            client.CloseDialog();
                            return;
                        }
                        var item = client.Aisling.HasItemReturnItem(classItem);
                        client.Aisling.Inventory.RemoveFromInventory(client, item);
                        client.Aisling._Dmg += 20;
                        client.Aisling._Hit += 20;
                        await Task.Delay(250).ContinueWith(ct =>
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings,
                                c => c.SendAnimation(303, null, client.Aisling.Serial));
                        });
                        await Task.Delay(250).ContinueWith(ct =>
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings,
                                c => c.SendSound(97, false));
                        });
                        await Task.Delay(450).ContinueWith(ct =>
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings,
                                c => c.SendAnimation(303, null, client.Aisling.Serial));
                        });
                        var legend = new Legend.LegendItem
                        {
                            Key = "Master",
                            Time = DateTime.UtcNow,
                            Color = LegendColor.TurquoiseG8,
                            Icon = (byte)LegendIcon.Victory,
                            Text = $"Mastery of the path of {client.Aisling.Path}"
                        };
                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.Aisling.Stage = ClassStage.Master;
                        client.SendAttributes(StatUpdateType.Full);
                        foreach (var announceClient in ServerSetup.Instance.Game.Aislings)
                        {
                            announceClient.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                                $"{{=c{client.Aisling.Username} has mastered the path of {client.Aisling.Path}");
                        }

                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x05, "Thank you")
                        };

                        client.SendOptionsDialog(Mundane,
                            "Outstanding work, you exhume the best of us! However, your road has just begun. There is a town called Rionnag, there master craftsmen can help you find new gear that would suit someone of your caliber better.",
                            options.ToArray());
                    }
                    else
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "You're missing the artifact");
                        client.CloseDialog();
                    }
                }
                break;
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

    public override void OnGossip(WorldClient client, string message) { }
}