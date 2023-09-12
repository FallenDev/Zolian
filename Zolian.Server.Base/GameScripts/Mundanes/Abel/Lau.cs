using System.Collections.Concurrent;

using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Lau")]
public class Lau : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Lau(WorldServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
        _spellList = ObtainSpellList();
    }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if ((client.Aisling.HasKilled("Undead Guard", 5) && client.Aisling.HasKilled("Undead Wizard", 5) && client.Aisling.QuestManager.Lau == 0) || client.Aisling.GameMaster)
        {
            options.Add(new(0x0B, "I have done what was asked"));
        }

        if (_skillList.Count > 0)
        {
            options.Add(new(0x01, "Show Available Skills"));
        }

        if (_spellList.Count > 0)
        {
            options.Add(new(0x0010, "Show Available Spells"));
        }

        options.Add(new(0x02, "Forget Skill"));
        options.Add(new(0x0011, "Forget Spell"));

        switch (client.Aisling.QuestManager.Lau)
        {
            case 0 when client.Aisling.Stage >= ClassStage.Master:
                options.Add(new(0x0A, "Warrior's Discipline"));
                break;
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Stage < ClassStage.Master
                ? "I have little patience for those who expect greatness out of dust"
                : "We have work to do, don't we kin?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var advExp = Random.Shared.Next(15000000, 25000000);

        switch (responseID)
        {
            #region Skills

            case 0x0001:
                {
                    var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSkills = _skillList.Except(learnedSkills).Where(i => i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.Path)
                        || i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.Path)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                        || i.Prerequisites.ClassRequired.ClassFlagIsSet(Class.Peasant)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(Class.Peasant)).ToList();

                    newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSkills.Count > 0)
                    {
                        client.SendSkillLearnDialog(Mundane, "What move do you wish to learn? \nThese skills have been taught for generations now and are available to you.", 0x0003, newSkills);
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
                    }

                    break;
                }
            case 0x0002:
                {
                    client.SendForgetSkills(Mundane,
                        "Muscle memory is a hard thing to unlearn. \nYou may come back to relearn what the mind has lost but the muscle still remembers.", 0x9000);
                    break;
                }
            case 0x9000:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "You don't quite have that skill.");
                        client.CloseDialog();
                    }

                    client.Aisling.SkillBook.Remove(client, (byte)idx);
                    client.LoadSkillBook();

                    client.SendForgetSkills(Mundane,
                        "Your body is still, breathing in, relaxed. \nAny other skills you wish to forget?", 0x9000);
                    break;
                }
            case 0x0003:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x0006, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x0004, "Learn"),
                        new Dialog.OptionsDataItem(0x0001, "No, thank you."));
                    break;
                }
            case 0x0004:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                    if (subject == null) return;

                    var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
                    {
                        if (!result)
                        {
                            client.SendOptionsDialog(Mundane, msg, subject.Name);
                        }
                    });

                    if (conditions)
                    {
                        client.SendOptionsDialog(Mundane, "Have you brought what is required?",
                            subject.Name,
                            new Dialog.OptionsDataItem(0x0005, "Yes."),
                            new Dialog.OptionsDataItem(0x0001, "I'll come back later."));
                    }

                    break;
                }
            case 0x0006:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                    if (subject == null) return;

                    client.SendOptionsDialog(Mundane,
                        $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                        subject.Name,
                        new Dialog.OptionsDataItem(0x0004, "Yes"),
                        new Dialog.OptionsDataItem(0x0001, "No"));

                    break;
                }
            case 0x0005:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                    if (subject == null) return;

                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(109, null, client.Aisling.Serial));
                    client.LearnSkill(Mundane, subject, "Always refine your skills as much as you sharpen your knife.");

                    break;
                }

            #endregion

            #region Spells

            case 0x0010:
                {
                    var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.Path)
                        || i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.Path)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                        || i.Prerequisites.ClassRequired.ClassFlagIsSet(Class.Peasant)
                        || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(Class.Peasant)).ToList();

                    newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSpells.Count > 0)
                    {
                        client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x0012, newSpells);
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
                    }

                    break;
                }
            case 0x0011:
                {
                    client.SendForgetSpells(Mundane, "A warrior's heart is vast, even we lose our way. \nBe warned, This cannot be undone.", 0x0800);
                    break;
                }
            case 0x0012:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x0015, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x0013, "Learn"),
                        new Dialog.OptionsDataItem(0x0010, "No, thank you."));
                    break;
                }
            case 0x0013:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;

                    var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
                    {
                        if (!result)
                        {
                            client.SendOptionsDialog(Mundane, msg, subject.Name);
                        }
                    });

                    if (conditions)
                    {
                        client.SendOptionsDialog(Mundane, "Have you brought what is required?",
                            subject.Name,
                            new Dialog.OptionsDataItem(0x0014, "Yes."),
                            new Dialog.OptionsDataItem(0x0010, "I'll come back later."));
                    }

                    break;
                }
            case 0x0014:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;

                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(109, null, client.Aisling.Serial));
                    client.LearnSpell(Mundane, subject, "Always expand your knowledge, Aisling.");

                    break;
                }
            case 0x0015:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;

                    client.SendOptionsDialog(Mundane,
                        $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                        subject.Name,
                        new Dialog.OptionsDataItem(0x0013, "Yes"),
                        new Dialog.OptionsDataItem(0x0010, "No"));

                    break;
                }
            case 0x0800:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I do not sense this spell within you any longer.");
                        client.CloseDialog();
                    }

                    client.Aisling.SpellBook.Remove(client, (byte)idx);
                    client.LoadSpellBook();

                    client.SendForgetSpells(Mundane, "It has been removed.\nRemember, This cannot be undone.", 0x0800);
                    break;
                }

            #endregion

            case 0x0008:
                {
                    client.SendOptionsDialog(Mundane, "When you're ready, I'll be here.");
                    break;
                }
            case 0x0009:
                {
                    client.SendOptionsDialog(Mundane, $"{{=aHead to Tagor's Necropolis and slay {{=c5{{=a Undead Guards, and {{=c5{{=a Undead Wizards");
                    break;
                }

            #region Warrior's Discipline

            case 0x000A:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x0009, "Sure."),
                        new (0x0008, "Not right now.")
                    };

                    client.SendOptionsDialog(Mundane, $"To learn what I have to teach, I expect you to kill something for me", options.ToArray());
                    break;
                }
            case 0x000B:
                {
                    if ((client.Aisling.HasKilled("Undead Guard", 5) && client.Aisling.HasKilled("Undead Wizard", 5) && client.Aisling.QuestManager.Lau == 0) || client.Aisling.GameMaster)
                    {
                        // Logic to give quest based on secondary class first
                        if (client.Aisling.Path == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Lau == 0)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Retribution", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Lau++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Indeed you are worthy, now pay close attention. I will not teach it again.");

                                if (client.Aisling.QuestManager.Lau == 1)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "LLau1",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Lau's Training (Retribution)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                                break;
                            }
                        }

                        if (client.Aisling.Path == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Lau == 0)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Vampiric Slash", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Lau++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Indeed you are worthy, now pay close attention. I will not teach it again.");

                                if (client.Aisling.QuestManager.Lau == 1)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "LLau1",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Lau's Training (Vampiric Slash)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                                break;
                            }
                        }

                        // Logic to complete quest if past class qualifies
                        if (client.Aisling.PastClass == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Lau == 0)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Retribution", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Lau++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Indeed you are worthy, now pay close attention. I will not teach it again.");

                                if (client.Aisling.QuestManager.Lau == 1)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "LLau1",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Lau's Training (Retribution)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Lau == 0)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Vampiric Slash", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Lau++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Indeed you are worthy, now pay close attention. I will not teach it again.");

                                if (client.Aisling.QuestManager.Lau == 1)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "LLau1",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Lau's Training (Vampiric Slash)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You have not finished your task, head to Tagor and defeat 5 Undead Guards and 5 Undead Wizards");
                    }

                    break;
                }

                #endregion
        }
    }
}