using System.Collections.Concurrent;
using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using ServiceStack;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Neal")]
public class Neal : MundaneScript
{
    private string _kill;
    private string _advKill;
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Neal(WorldServer server, Mundane mundane) : base(server, mundane)
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

        if (!client.Aisling.QuestManager.NealKill.IsNullOrEmpty() || client.Aisling.GameMaster)
        {
            switch (client.Aisling.QuestManager.Neal)
            {
                case <= 1 when client.Aisling.Level >= 3 || client.Aisling.GameMaster:
                    options.Add(new(0x0B, $"{{=cI have attempted the trial{{=a."));
                    break;
                case 2 when client.Aisling.Level >= 50 || client.Aisling.GameMaster:
                    options.Add(new(0x0D, $"{{=cI have attempted the trial{{=a."));
                    break;
                case 3 when client.Aisling.Level >= 80 || client.Aisling.GameMaster:
                    options.Add(new(0x0F, $"{{=cI have attempted the trial{{=a."));
                    break;
            }
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

        if (client.Aisling.QuestManager.NealKill.IsNullOrEmpty() && (client.Aisling.Path is Class.Berserker or Class.Defender || client.Aisling.PastClass is Class.Berserker or Class.Defender) || client.Aisling.GameMaster)
        {
            switch (client.Aisling.QuestManager.Neal)
            {
                case <= 1 when client.Aisling.Level >= 3:
                    options.Add(new(0x0A, "A Warrior's Trial"));
                    break;
                case 2 when client.Aisling.Level >= 50:
                    options.Add(new(0x0C, "Advanced Training"));
                    break;
                case 3 when client.Aisling.Level >= 80:
                    options.Add(new(0x0E, "Turbulent Combat"));
                    break;
            }
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level <= 98
                ? "You look puny! Is this what they're sending me now? Well, let's get to work."
                : "Hail, let's get to work.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var warriorThings = Random.Shared.Next(1, 5);
        var countMon = Random.Shared.Next(6, 10);
        var advExp = (uint)Random.Shared.Next(20000, 25000);
        var advExp2 = (uint)Random.Shared.Next(750000, 1000000);
        var advExp3 = (uint)Random.Shared.Next(3000000, 4500000);

        _kill = warriorThings switch
        {
            1 => "Spider",
            2 => "Mouse",
            3 => "Centipede",
            4 => "Bat",
            _ => _kill
        };

        _advKill = warriorThings switch
        {
            1 => "Marauder",
            2 => "Kardi",
            3 => "White Bat",
            4 => "Mimic",
            _ => _advKill
        };

        switch (responseID)
        {
            #region Skills

            case 0x0001:
                {
                    var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSkills = _skillList.Except(learnedSkills).ToList();

                    newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSkills.Count > 0)
                    {
                        client.SendSkillLearnDialog(Mundane, "What move do you wish to learn? \nThese skills have been taught for generations now and are available to you.", 0x0003,
                            newSkills.Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                                 || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                                 || i.Prerequisites.ClassRequired == Class.Peasant));
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

                    client.Aisling.SkillBook.Remove(client, (byte)idx, true);
                    client.SendRemoveSkillFromPane((byte)idx);
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

                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(109, client.Aisling.Serial));
                    client.LearnSkill(Mundane, subject, "Always refine your skills as much as you sharpen your knife.");

                    break;
                }

            #endregion

            #region Spells

            case 0x0010:
                {
                    var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSpells = _spellList.Except(learnedSpells).ToList();

                    newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSpells.Count > 0)
                    {
                        client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x0012,
                            newSpells.Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                                 || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                                 || i.Prerequisites.ClassRequired == Class.Peasant));
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

                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(109, client.Aisling.Serial));
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

                    client.Aisling.SpellBook.Remove(client, (byte)idx, true);
                    client.SendRemoveSpellFromPane((byte)idx);
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
                    if (client.Aisling.QuestManager.NealKill.IsNullOrEmpty())
                    {
                        client.Aisling.QuestManager.NealKill = _kill;
                        client.Aisling.QuestManager.NealCount = countMon;
                    }

                    client.SendOptionsDialog(Mundane, $"Make it back with your limbs intact.\n{{=qKill: {countMon}, {_kill}'s");
                    break;
                }

            #region A Warrior's Trial

            case 0x000A:
                {
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0009, "Sure."),
                    new (0x0008, "Not right now.")
                };

                    client.SendOptionsDialog(Mundane, $"This trial will test your might against a foe of my choosing.", options.ToArray());
                    break;
                }
            case 0x000B:
                {
                    if (client.Aisling.HasKilled(client.Aisling.QuestManager.NealKill, client.Aisling.QuestManager.NealCount) || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.Path == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 0)
                            {
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(8000);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 8,000 experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Crypts have been getting dangerous, thank you for taking care of that.",
                                    new Dialog.OptionsDataItem(0x000A, "Yes."),
                                    new Dialog.OptionsDataItem(0x0008, "I'll come back later."));

                                break;
                            }

                            if (client.Aisling.QuestManager.Neal == 1)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Aid", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "You've passed your {=qsecond{=a trial. For that, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 2)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Aid)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.Path == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 0)
                            {
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(8000);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 8,000 experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Crypts have been getting dangerous, thank you for taking care of that.",
                                    new Dialog.OptionsDataItem(0x000A, "Yes."),
                                    new Dialog.OptionsDataItem(0x0008, "I'll come back later."));

                                break;
                            }

                            if (client.Aisling.QuestManager.Neal == 1)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Rescue", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "You've passed your {=qsecond{=a trial. For that, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 2)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Rescue)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 0)
                            {
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(8000);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 8,000 experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Crypts have been getting dangerous, thank you for taking care of that.",
                                    new Dialog.OptionsDataItem(0x000A, "Yes."),
                                    new Dialog.OptionsDataItem(0x0008, "I'll come back later."));

                                break;
                            }

                            if (client.Aisling.QuestManager.Neal == 1)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Aid", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "You've passed your {=qsecond{=a trial. For that, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 2)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Aid)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 0)
                            {
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(8000);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 8,000 experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Crypts have been getting dangerous, thank you for taking care of that.",
                                    new Dialog.OptionsDataItem(0x000A, "Yes."),
                                    new Dialog.OptionsDataItem(0x0008, "I'll come back later."));

                                break;
                            }

                            if (client.Aisling.QuestManager.Neal == 1)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Rescue", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "You've passed your {=qsecond{=a trial. For that, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 2)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Rescue)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                            }
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You have not finished your task, eliminate more of these creatures: {{=q{client.Aisling.QuestManager.NealKill}");
                    }

                    break;
                }

            #endregion

            #region Advanced Training

            case 0x000C:
                {
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0016, "Sure."),
                    new (0x0008, "Not right now.")
                };

                    client.SendOptionsDialog(Mundane, $"This trial is more advanced and will let me know if you're ready for the next phase.", options.ToArray());
                    break;
                }
            case 0x000D:
                {
                    if (client.Aisling.HasKilled(client.Aisling.QuestManager.NealKill, client.Aisling.QuestManager.NealCount) || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.Path == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 2)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Lullaby Strike", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp2);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Good aisling, you have shown real talent, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 3)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Lullaby Strike)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.Path == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 2)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Raise Threat", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp2);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Good aisling, you have shown real talent, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 3)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Raise Threat)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 2)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Lullaby Strike", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp2);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Good aisling, you have shown real talent, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 3)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Lullaby Strike)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 2)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Raise Threat", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp2);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Good aisling, you have shown real talent, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 3)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Raise Threat)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                            }
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You have not finished your task, eliminate more of these creatures: {{=q{client.Aisling.QuestManager.NealKill}");
                    }

                    break;
                }

            #endregion

            #region Turbulent Combat

            case 0x000E:
                {
                    var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0017, "Sure."),
                    new (0x0008, "Not right now.")
                };

                    client.SendOptionsDialog(Mundane, $"This is your final trial. If you pass, I'll teach you something special.\nFor your trial I want you to kill {{=q5 {{=bWraith's{{=a.", options.ToArray());

                    break;
                }
            case 0x000F:
                {
                    if (client.Aisling.HasKilled("Wraith", 5) || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.Path == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 3)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Desolate", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp3);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp3} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Revered one, as promised, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Desolate)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Adventure",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Yellow,
                                        Icon = (byte)LegendIcon.Victory,
                                        Value = "Finished Neal's Training"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.Path == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 3)
                            {
                                var spell = Spell.GiveTo(client.Aisling, "Defensive Stance", 1);
                                if (spell) client.LoadSpellBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp3);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp3} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Revered one, as promised, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Spell",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Peony,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Defensive Stance)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Adventure",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Yellow,
                                        Icon = (byte)LegendIcon.Victory,
                                        Value = "Finished Neal's Training"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Berserker)
                        {
                            if (client.Aisling.QuestManager.Neal == 3)
                            {
                                var skill = Skill.GiveTo(client.Aisling, "Desolate", 1);
                                if (skill) client.LoadSkillBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp3);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp3} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Revered one, as promised, let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Skill",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Pink,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Desolate)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Adventure",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Yellow,
                                        Icon = (byte)LegendIcon.Victory,
                                        Value = "Finished Neal's Training"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                break;
                            }
                        }

                        if (client.Aisling.PastClass == Class.Defender)
                        {
                            if (client.Aisling.QuestManager.Neal == 3)
                            {
                                var spell = Spell.GiveTo(client.Aisling, "Defensive Stance", 1);
                                if (spell) client.LoadSpellBook();
                                client.Aisling.QuestManager.Neal++;
                                client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                                client.Aisling.QuestManager.NealKill = null;
                                client.Aisling.QuestManager.NealCount = 0;
                                client.GiveExp(advExp3);
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp3} experience.");
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.SendOptionsDialog(Mundane, "Revered one, as promised, now let me teach you something.");

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Spell",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Peony,
                                        Icon = (byte)LegendIcon.Warrior,
                                        Value = "Neal's Training (Defensive Stance)"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }

                                if (client.Aisling.QuestManager.Neal == 4)
                                {
                                    var item = new Legend.LegendItem
                                    {
                                        Category = "Adventure",
                                        Time = DateTime.UtcNow,
                                        Color = LegendColor.Yellow,
                                        Icon = (byte)LegendIcon.Victory,
                                        Value = "Finished Neal's Training"
                                    };

                                    client.Aisling.LegendBook.AddLegend(item, client);
                                }
                            }
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"You have not finished your task, eliminate a {{=bWraith {{=athen come see me.");
                    }

                    break;
                }

            #endregion

            case 0x0016:
                {
                    if (client.Aisling.QuestManager.NealKill.IsNullOrEmpty())
                    {
                        client.Aisling.QuestManager.NealKill = _advKill;
                        client.Aisling.QuestManager.NealCount = countMon;
                    }

                    client.SendOptionsDialog(Mundane, $"Make it back, head held high!\n{{=qKill: {countMon}, {_advKill}'s");
                    break;
                }
            case 0x0017:
                {
                    if (client.Aisling.QuestManager.NealKill.IsNullOrEmpty())
                    {
                        client.Aisling.QuestManager.NealKill = "Wraith";
                        client.Aisling.QuestManager.NealCount = 5;
                    }

                    client.SendOptionsDialog(Mundane, $"Becareful out there.\n{{=qKill: {client.Aisling.QuestManager.NealCount}, {client.Aisling.QuestManager.NealKill}'s");
                    break;
                }
        }
    }
}