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

using ServiceStack;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Keela")]
public class Keela : MundaneScript
{
    private string _kill;
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Keela(WorldServer server, Mundane mundane) : base(server, mundane)
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

        if (client.Aisling.QuestManager.Keela <= 1 && client.Aisling.QuestManager.KeelaQuesting)
        {
            switch (client.Aisling.QuestManager.Keela)
            {
                case 0 when client.Aisling.Level >= 3 || client.Aisling.GameMaster:
                    options.Add(new(0x0B, $"{{=cI have returned{{=a."));
                    break;
                case 1 when client.Aisling.Level >= 30 || client.Aisling.GameMaster:
                    options.Add(new(0x0D, $"{{=cI found the path{{=a."));
                    break;
            }
        }

        if (!client.Aisling.QuestManager.KeelaKill.IsNullOrEmpty() && client.Aisling.QuestManager.Keela > 1)
            options.Add(new(0x0F, $"{{=cI have returned{{=a."));

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

        if (client.Aisling.QuestManager.KeelaKill.IsNullOrEmpty() && (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin || client.Aisling.GameMaster))
        {
            switch (client.Aisling.QuestManager.Keela)
            {
                case 0 when client.Aisling.Level >= 6:
                    options.Add(new(0x0A, "Mischievous Deeds"));
                    break;
                case 1 when client.Aisling.Level >= 30:
                    options.Add(new(0x0C, "Hidden Paths"));
                    break;
                case 2 when client.Aisling.Level >= 71:
                    options.Add(new(0x0E, "Detrimental Exploits"));
                    break;
            }
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level <= 98
                ? "Did you come alone?"
                : "Always walk in the shadows my dear friend.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var assassinThings = Random.Shared.Next(1, 5);
        var countMon = Random.Shared.Next(6, 10);
        var advExp = Random.Shared.Next(20000, 25000);
        var advExp2 = Random.Shared.Next(750000, 1000000);
        var advExp3 = Random.Shared.Next(3000000, 4500000);

        _kill = assassinThings switch
        {
            1 => "Mantis",
            2 => "Bat",
            3 => "Centipede",
            4 => "Honey Bee",
            _ => _kill
        };

        switch (responseID)
        {
            #region Skills

            case 0x0001:
            {
                var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                var newSkills = _skillList.Except(learnedSkills).Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                                                            || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                                                            || i.Prerequisites.ClassRequired == Class.Peasant).ToList();

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

                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(109, client.Aisling.Serial));
                client.LearnSkill(Mundane, subject, "Always refine your skills as much as you sharpen your knife.");

                break;
            }

            #endregion

            #region Spells

            case 0x0010:
            {
                var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                                                            || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                                                            || i.Prerequisites.ClassRequired == Class.Peasant).ToList();

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
                client.SendForgetSpells(Mundane, "Every road leads to a different path. \nBe warned, This cannot be undone.", 0x0800);
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

                client.Aisling.SpellBook.Remove(client, (byte)idx);
                client.LoadSpellBook();

                client.SendForgetSpells(Mundane, "It is done.\nRemember, This cannot be undone.", 0x0800);
                break;
            }

            #endregion

            case 0x0008:
            {
                client.SendOptionsDialog(Mundane, "Like a still pond, I am but mist over a shallow mountain.");
                break;
            }
            case 0x0009:
            {
                if (client.Aisling.QuestManager.KeelaKill.IsNullOrEmpty())
                {
                    client.Aisling.QuestManager.KeelaKill = _kill;
                    client.Aisling.QuestManager.KeelaCount = countMon;
                }

                client.SendOptionsDialog(Mundane, $"Do not be seen. Leave no trace.\n{{=qKill: {countMon}, {_kill}'s");
                client.Aisling.QuestManager.KeelaQuesting = true;
                break;
            }

            #region Mischievous Deeds

            case 0x000A:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0009, "Sure."),
                    new (0x0008, "Not right now.")
                };

                client.SendOptionsDialog(Mundane, $"I want you to slay some monsters to test your worth.", options.ToArray());
                break;
            }
            case 0x000B:
            {
                if (client.Aisling.HasKilled(client.Aisling.QuestManager.KeelaKill, client.Aisling.QuestManager.KeelaCount) || client.Aisling.GameMaster)
                {
                    if (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.QuestManager.Keela == 0)
                        {
                            client.Aisling.QuestManager.Keela++;
                            client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                            client.Aisling.QuestManager.KeelaKill = null;
                            client.Aisling.QuestManager.KeelaCount = 0;
                            client.Aisling.QuestManager.KeelaQuesting = false;
                            client.GiveExp(advExp);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.SendOptionsDialog(Mundane, "Tougher than I thought you were.");
                        }
                    }
                }
                else
                {
                    client.SendOptionsDialog(Mundane, $"You have not finished your task, eliminate more of these creatures: {{=q{client.Aisling.QuestManager.KeelaKill}");
                }

                break;
            }

            #endregion

            #region Hidden Paths

            case 0x000C:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0016, "Sure."),
                    new (0x0008, "Not right now.")
                };

                client.SendOptionsDialog(Mundane, "There is a hidden store for us in Abel, find it.", options.ToArray());
                break;
            }
            case 0x000D:
            {
                if (client.Aisling.HasItem("Assassin Notes"))
                {
                    if (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.QuestManager.Keela == 1)
                        {
                            var item = client.Aisling.HasItemReturnItem("Assassin Notes");
                            if (item == null) TopMenu(client);
                            client.Aisling.Inventory.RemoveFromInventory(client, item);

                            var skill = Skill.GiveTo(client.Aisling, "Sneak", 1);
                            if (skill) client.LoadSkillBook();
                            client.Aisling.QuestManager.Keela++;
                            client.Aisling.QuestManager.KeelaQuesting = false;
                            client.GiveExp(advExp2);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.SendOptionsDialog(Mundane, "Ahh, so that's what Teegan wants done. Come here, let me teach you something useful.");

                            if (client.Aisling.QuestManager.Keela == 2)
                            {
                                var legend = new Legend.LegendItem
                                {
                                    Category = "Skill",
                                    Time = DateTime.UtcNow,
                                    Color = LegendColor.Pink,
                                    Icon = (byte)LegendIcon.Rogue,
                                    Value = "Keela's Training (Sneak)"
                                };

                                client.Aisling.LegendBook.AddLegend(legend, client);
                            }
                        }
                    }
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "You haven't visited the hidden store just yet. You'll find it in Abel.");
                }

                break;
            }

            #endregion

            #region Detrimental Exploits

            case 0x000E:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x0017, "Sure."),
                    new (0x0008, "Not right now.")
                };

                client.SendOptionsDialog(Mundane, "I want you to eliminate 5 Succubus.", options.ToArray());

                break;
            }
            case 0x000F:
            {
                if (client.Aisling.HasKilled("Succubus", 5))
                {
                    if (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin || client.Aisling.GameMaster)
                    {
                        if (client.Aisling.QuestManager.Keela == 2)
                        {
                            var skill = Skill.GiveTo(client.Aisling, "Shadow Step", 1);
                            if (skill) client.LoadSkillBook();
                            client.Aisling.QuestManager.Keela++;
                            client.Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
                            client.Aisling.QuestManager.KeelaKill = null;
                            client.Aisling.QuestManager.KeelaCount = 0;
                            client.Aisling.QuestManager.KeelaQuesting = false;
                            client.GiveExp(advExp3);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp3} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.SendOptionsDialog(Mundane, "Always walk in the shadows, indeed, let me show you a technique.");

                            if (client.Aisling.QuestManager.Keela == 3)
                            {
                                var legend = new Legend.LegendItem
                                {
                                    Category = "Skill",
                                    Time = DateTime.UtcNow,
                                    Color = LegendColor.Pink,
                                    Icon = (byte)LegendIcon.Rogue,
                                    Value = "Keela's Training (Shadow Step)"
                                };

                                client.Aisling.LegendBook.AddLegend(legend, client);
                            }
                        }
                    }
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "You have not finished your task, eliminate three succubi.");
                }

                if (client.Aisling.QuestManager.Keela == 3)
                {
                    var item = new Legend.LegendItem
                    {
                        Category = "Adventure",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Yellow,
                        Icon = (byte)LegendIcon.Victory,
                        Value = "Walks in the Shadows"
                    };

                    client.Aisling.LegendBook.AddLegend(item, client);
                }

                break;
            }

            #endregion

            case 0x0016:
            {
                client.SendOptionsDialog(Mundane, "When you find it, return to me.");
                client.Aisling.QuestManager.KeelaQuesting = true;
                break;
            }
            case 0x0017:
            {
                if (client.Aisling.QuestManager.KeelaKill.IsNullOrEmpty())
                {
                    client.Aisling.QuestManager.KeelaKill = "Succubus";
                    client.Aisling.QuestManager.KeelaCount = 5;
                }

                client.SendOptionsDialog(Mundane, $"Becareful out there.\n{{=qKill: {client.Aisling.QuestManager.KeelaCount}, {client.Aisling.QuestManager.KeelaKill}'s");
                client.Aisling.QuestManager.KeelaQuesting = true;
                break;
            }
        }
    }
}