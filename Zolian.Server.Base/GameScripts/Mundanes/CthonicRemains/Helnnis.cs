using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using ServiceStack;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Advent Guild Leader")]
public class Helnnis(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private string _kill;
    private string _find;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        _kill = "";
        _find = "";
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.CthonicKillCompletions <= 3 && client.Aisling.QuestManager.CthonicKillTarget.IsEmpty())
            options.Add(new Dialog.OptionsDataItem(0x02, "Hunting Tasks"));

        if (!client.Aisling.QuestManager.CthonicKillTarget.IsEmpty())
            options.Add(new Dialog.OptionsDataItem(0x03, $"{{=bI've killed the target"));

        if (client.Aisling.QuestManager.AdventuresGuildReputation is >= 3 and <= 6 && client.Aisling.QuestManager.CthonicFindTarget.IsEmpty())
            options.Add(new Dialog.OptionsDataItem(0x04, "Gathering Tasks"));

        if (!client.Aisling.QuestManager.CthonicFindTarget.IsEmpty())
            options.Add(new Dialog.OptionsDataItem(0x05, $"{{=bI've brought the item requested"));

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 6
            && !client.Aisling.QuestManager.CthonicCleansingOne)
            options.Add(new Dialog.OptionsDataItem(0x21, "Cthonic Depths Cleansing"));

        if (client.Aisling.HasKilled("Void Cleric", 1) && !client.Aisling.QuestManager.CthonicCleansingOne)
            options.Add(new Dialog.OptionsDataItem(0x22, $"{{=bI have slain the cleric"));

        if (client.Aisling.QuestManager.CthonicCleansingOne && !client.Aisling.QuestManager.CthonicCleansingTwo)
            options.Add(new Dialog.OptionsDataItem(0x23, "Deeper Cleansing"));

        if (client.Aisling.HasKilled("Lich Dragon", 1) && !client.Aisling.QuestManager.CthonicCleansingTwo)
            options.Add(new Dialog.OptionsDataItem(0x24, $"{{=bI have slain the dragon"));

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 5)
            switch (client.Aisling.QuestManager.CthonicRemainsExplorationLevel)
            {
                case 0:
                    options.Add(new Dialog.OptionsDataItem(0x15, "Depths 5 Exploration"));
                    break;
                case 1:
                    options.Add(new Dialog.OptionsDataItem(0x17, "Depths 12 Exploration"));
                    break;
                case 2:
                    //options.Add(new Dialog.OptionsDataItem(0x19, "Visit Forward SpecOp Camp"));
                    break;
            }

        if (client.Aisling.HasVisitedMap(5036) && client.Aisling.QuestManager.CthonicRemainsExplorationLevel == 0)
            options.Add(new Dialog.OptionsDataItem(0x16, $"{{=bReport Back"));

        if (client.Aisling.HasVisitedMap(5043) && client.Aisling.QuestManager.CthonicRemainsExplorationLevel == 1)
            options.Add(new Dialog.OptionsDataItem(0x18, $"{{=bReport Back"));

        if (client.Aisling.HasVisitedMap(5050) && client.Aisling.QuestManager.CthonicRemainsExplorationLevel == 2)
            options.Add(new Dialog.OptionsDataItem(0x20, $"{{=bReport Back"));

        if (!client.Aisling.QuestManager.CthonicRuinsAccess
            && client.Aisling.QuestManager.CthonicDepthsCleansing
            && client.Aisling.QuestManager.CthonicRemainsExplorationLevel == 3)
            options.Add(new Dialog.OptionsDataItem(0x30, "Cthonic Ruins Access"));

        client.SendOptionsDialog(Mundane, "Ah, an able body! Come over here! We have much to discuss.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        var randCounter = Random.Shared.Next(1, 12);
        var huntingExp = Random.Shared.Next(150000000, 300000000);
        var gatheringExp = Random.Shared.Next(200000000, 500000000);

        if (client.Aisling.QuestManager.CthonicKillTarget.IsEmpty())
            _kill = randCounter switch
            {
                1 => "Dhole Larva",
                2 => "Nagetier Dieter",
                3 => "Scarlet Beetle",
                4 => "Qualgeist",
                5 => "Bone Scorpion",
                6 => "Mummy",
                7 => "Shambler",
                8 => "Golem",
                9 => "Dark Cleric",
                10 => "Unseelie Satyr",
                11 => "Fomorian Horror",
                12 => "Kopfloser Reiter",
                _ => "Dark Cleric"
            };
        else
            _kill = client.Aisling.QuestManager.CthonicKillTarget;

        if (client.Aisling.QuestManager.CthonicFindTarget.IsEmpty())
            _find = randCounter switch
            {
                1 => "Nagetier's Talon",
                2 => "Scarlet Beetle Antenna",
                3 => "Qualgeist's Head",
                4 => "Ancient Bones",
                5 => "Mummy Bandage",
                6 => "Golem Flesh",
                7 => "Satyr's Hoof",
                8 => "Fomorian Rag",
                9 => "Golem's Eye",
                10 => "Golem's Bandages",
                _ => "Ancient Bones"
            };
        else
            _find = client.Aisling.QuestManager.CthonicFindTarget;

        switch (responseId)
        {
            case 0x00:
                client.CloseDialog();
                break;
            case 0x02:
                client.SendOptionsDialog(Mundane, $"You must hunt down a {_kill}. Upon completion, I will move you up our roster.", new Dialog.OptionsDataItem(0x00, "Expect it done."));
                client.Aisling.QuestManager.CthonicKillTarget = _kill;
                break;
            case 0x03:
                {
                    if (client.Aisling.QuestManager.CthonicKillTarget.IsEmpty())
                    {
                        client.SendOptionsDialog(Mundane, "You have not accepted this task. Please return when you have.", new Dialog.OptionsDataItem(0x00, "I will return."));
                        return;
                    }

                    if (client.Aisling.HasKilled(client.Aisling.QuestManager.CthonicKillTarget, 1))
                    {
                        client.SendOptionsDialog(Mundane, "You have slain the target. Well done! You will be rewarded with " + huntingExp +
                                                          " experience points.", new Dialog.OptionsDataItem(0x00, "Thank you."));
                        client.Aisling.QuestManager.CthonicKillTarget = string.Empty;
                        client.Aisling.QuestManager.CthonicKillCompletions += 1;
                        client.Aisling.QuestManager.AdventuresGuildReputation += 1;
                        client.Aisling.MonsterKillCounters.Clear();
                        client.GiveExp(huntingExp);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You must hunt down a {client.Aisling.QuestManager.CthonicKillTarget}. Upon completion, I will move you up our roster.", new Dialog.OptionsDataItem(0x00, "I will do it."));
                    }
                }
                break;
            case 0x04:
                client.SendOptionsDialog(Mundane, $"You must gather {_find} x 3 items. Bring them back to me, and receive the guild's blessing.", new Dialog.OptionsDataItem(0x00, "I'll bring what I find."));
                client.Aisling.QuestManager.CthonicFindTarget = _find;
                break;
            case 0x05:
                {
                    if (client.Aisling.QuestManager.CthonicFindTarget.IsEmpty())
                    {
                        client.SendOptionsDialog(Mundane, "You have not accepted this task. Please return when you have.", new Dialog.OptionsDataItem(0x00, "I will return."));
                        return;
                    }

                    if (client.Aisling.HasInInventory(client.Aisling.QuestManager.CthonicFindTarget, 3))
                    {
                        client.SendOptionsDialog(Mundane, "You have gathered the items. Well done! You will be rewarded with " + gatheringExp +
                                                          " experience points.", new Dialog.OptionsDataItem(0x00, "Thank you."));
                        var item = client.Aisling.HasItemReturnItem(client.Aisling.QuestManager.CthonicFindTarget);
                        client.Aisling.Inventory.RemoveRange(client, item, 3);
                        client.Aisling.QuestManager.CthonicFindTarget = string.Empty;
                        client.Aisling.QuestManager.AdventuresGuildReputation += 1;
                        client.Aisling.MonsterKillCounters.Clear();
                        client.GiveExp(gatheringExp);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You must gather {_find} x 3 items. Bring them back to me, and receive the guild's blessing.", new Dialog.OptionsDataItem(0x00, "I will do it."));
                    }
                }
                break;
            case 0x15:
                {
                    client.SendOptionsDialog(Mundane, "Make your way down to Cthonic Depths 5. Then report back to me.", new Dialog.OptionsDataItem(0x00, "See you soon."));
                }
                break;
            case 0x16:
                {
                    if (client.Aisling.HasVisitedMap(5036))
                    {
                        client.SendOptionsDialog(Mundane, "Ah, so the situation is getting serious. I wonder how it is deeper near floor 12?");
                        client.Aisling.QuestManager.CthonicRemainsExplorationLevel = 1;
                        client.GiveExp(5000000);
                    }
                    else
                        client.SendOptionsDialog(Mundane, "Come back to me once you've visited Cthonic Depths 5.");
                }
                break;
            case 0x17:
                {
                    client.SendOptionsDialog(Mundane, "Make your way down to Cthonic Depths 12. Then report back to me.", new Dialog.OptionsDataItem(0x00, "See you soon."));
                }
                break;
            case 0x18:
                {
                    if (client.Aisling.HasVisitedMap(5043))
                    {
                        client.SendOptionsDialog(Mundane, "Hmm a pincher formation, not good... I'm going to need you to check out if our team at the Forward SpecOp Camp are ok.");
                        client.Aisling.QuestManager.CthonicRemainsExplorationLevel = 2;
                        client.GiveExp(50000000);
                    }
                    else
                        client.SendOptionsDialog(Mundane, "Come back to me once you've visited Cthonic Depths 12.");
                }
                break;
            case 0x19:
                {
                    client.SendOptionsDialog(Mundane, "Make your way down to the Forward SpecOp Camp. Then report back to me.", new Dialog.OptionsDataItem(0x00, "Be back shortly"));
                }
                break;
            case 0x20:
                {
                    if (client.Aisling.HasVisitedMap(5050))
                    {
                        client.SendOptionsDialog(Mundane, "You've done well. I'm going to need you to check out the Cthonic Ruins. Be careful, the creatures there are not to be trifled with.");
                        client.Aisling.QuestManager.CthonicRemainsExplorationLevel = 3;
                        client.GiveExp(500000000);
                        var legend = new Legend.LegendItem
                        {
                            Key = "LAdvFSC",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.BlueG5,
                            Icon = (byte)LegendIcon.Heart,
                            Text = "Adventurer's Guild: Spec Operator"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                        client.SendOptionsDialog(Mundane, "Come back to me once you've visited the Forward SpecOp Camp.");
                }
                break;
            case 0x21:
                {
                    if (client.Aisling.QuestManager.CthonicCleansingOne)
                    {
                        client.SendOptionsDialog(Mundane, "You have already completed this task.", new Dialog.OptionsDataItem(0x00, "Alright"));
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "Deep within the Cthonic Depths, there is a Dark Cleric whose power emanates throughout the cavern. Deal with it.", new Dialog.OptionsDataItem(0x00, "I will end it."));
                }
                break;
            case 0x22:
                {
                    if (client.Aisling.QuestManager.CthonicCleansingOne)
                    {
                        client.SendOptionsDialog(Mundane, "You have already completed this task.", new Dialog.OptionsDataItem(0x00, "Alright"));
                        return;
                    }

                    if (client.Aisling.HasKilled("Void Cleric", 1))
                    {
                        client.SendOptionsDialog(Mundane, "You have slain the Void Cleric. Well done! The guild sleeps better at night, knowing that isn't lurking nearby.", new Dialog.OptionsDataItem(0x00, "You're welcome"));
                        client.Aisling.QuestManager.CthonicCleansingOne = true;
                        client.Aisling.QuestManager.AdventuresGuildReputation += 1;
                        var legend = new Legend.LegendItem
                        {
                            Key = "LAdvVC",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.Cyan,
                            Icon = (byte)LegendIcon.Victory,
                            Text = "Adventurer's Guild: Slain the Void Cleric"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "You must hunt down a Dark Cleric. Upon completion, I will move you up our roster.", new Dialog.OptionsDataItem(0x00, "I will do it."));
                    }
                }
                break;
            case 0x23:
                {
                    if (client.Aisling.QuestManager.CthonicCleansingTwo)
                    {
                        client.SendOptionsDialog(Mundane, "You have already completed this task.", new Dialog.OptionsDataItem(0x00, "Alright"));
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "Even further within the Cthonic Depths, there is a Lich Dragon that boosts the power of nearby foes. Deal with it.", new Dialog.OptionsDataItem(0x00, "I will end it."));
                }
                break;
            case 0x24:
                {
                    if (client.Aisling.QuestManager.CthonicCleansingTwo)
                    {
                        client.SendOptionsDialog(Mundane, "You have already completed this task.", new Dialog.OptionsDataItem(0x00, "Alright"));
                        return;
                    }

                    if (client.Aisling.HasKilled("Lich Dragon", 1))
                    {
                        client.SendOptionsDialog(Mundane, "You have slain the Lich Dragon. Well done! The guild sleeps better at night, knowing that isn't lurking nearby.", new Dialog.OptionsDataItem(0x00, "You're welcome"));
                        client.Aisling.QuestManager.CthonicCleansingTwo = true;
                        client.Aisling.QuestManager.CthonicDepthsCleansing = true;
                        client.Aisling.QuestManager.AdventuresGuildReputation += 1;
                        var legend = new Legend.LegendItem
                        {
                            Key = "LAdvLD",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.Cyan,
                            Icon = (byte)LegendIcon.Victory,
                            Text = "Adventurer's Guild: Slain the Lich Dragon"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "You must hunt down a Lich Dragon. Upon completion, I will move you up our roster.", new Dialog.OptionsDataItem(0x00, "I will do it."));
                    }
                }
                break;
            case 0x30:
                {
                    client.SendOptionsDialog(Mundane, "You have been granted access to the Cthonic Ruins. Be careful, we have not had an operator as talented as you in a long while.");
                    client.Aisling.QuestManager.CthonicRuinsAccess = true;
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}