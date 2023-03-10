using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using GameServer = Darkages.Network.Server.GameServer;

namespace Darkages.GameScripts.Mundanes.Rucesion;

[Script("Gowther")]
public class Gowther : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Gowther(GameServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
        _spellList = ObtainSpellList();
    }

    public override void OnClick(GameServer server, GameClient client)
    {
        TopMenu(client);
    }

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>();

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

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level <= 98
                ? "Find anything good?"
                : "Walk in the shadows my dear friend.", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

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
                    client.SendMessage(0x02, "I have nothing left to teach you, for now.");
                }

                break;
            }
            case 0x0002:
            {
                client.SendSkillForgetDialog(Mundane,
                    "Muscle memory is a hard thing to unlearn. \nYou may come back to relearn what the mind has lost but the muscle still remembers.", 0x9000);
                break;
            }
            case 0x9000:
            {
                int.TryParse(args, out var idx);

                if (idx is < 0 or > byte.MaxValue)
                {
                    client.SendMessage(0x02, "You don't quite have that skill.");
                    client.CloseDialog();
                }

                client.Aisling.SkillBook.Remove((byte)idx, true);
                client.Send(new ServerFormat2D((byte)idx));
                client.LoadSkillBook();

                client.SendSkillForgetDialog(Mundane,
                    "Your body is still, breathing in, relaxed. \nAny other skills you wish to forget?", 0x9000);
                break;
            }
            case 0x0003:
            {
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                    new OptionsDataItem(0x0006, $"What does {args} do?"),
                    new OptionsDataItem(0x0004, "Learn"),
                    new OptionsDataItem(0x0001, "No, thank you."));
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
                        new OptionsDataItem(0x0005, "Yes."),
                        new OptionsDataItem(0x0001, "I'll come back later."));
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
                    new OptionsDataItem(0x0004, "Yes"),
                    new OptionsDataItem(0x0001, "No"));

                break;
            }
            case 0x0005:
            {
                var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                if (subject == null) return;

                client.SendAnimation(109, client.Aisling, Mundane);
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
                    client.SendMessage(0x02, "I have nothing left to teach you, for now.");
                }

                break;
            }
            case 0x0011:
            {
                client.SendSpellForgetDialog(Mundane, "Unlearning spells is a dangerous process, sometimes we lose ourselves. \nBe warned, This cannot be undone.", 0x0800);
                break;
            }
            case 0x0012:
            {
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                    new OptionsDataItem(0x0015, $"What does {args} do?"),
                    new OptionsDataItem(0x0013, "Learn"),
                    new OptionsDataItem(0x0010, "No, thank you."));
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
                        new OptionsDataItem(0x0014, "Yes."),
                        new OptionsDataItem(0x0010, "I'll come back later."));
                }

                break;
            }
            case 0x0014:
            {
                var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                if (subject == null) return;

                client.SendAnimation(109, client.Aisling, Mundane);
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
                    new OptionsDataItem(0x0013, "Yes"),
                    new OptionsDataItem(0x0010, "No"));

                break;
            }
            case 0x0800:
            {
                int.TryParse(args, out var idx);

                if (idx is < 0 or > byte.MaxValue)
                {
                    client.SendMessage(0x02, "I do not sense this spell within you any longer.");
                    client.CloseDialog();
                }

                client.Aisling.SpellBook.Remove((byte)idx, true);
                client.Send(new ServerFormat18((byte)idx));
                client.LoadSpellBook();

                client.SendSpellForgetDialog(Mundane, "Shall we attempt more?\nRemember, This cannot be undone.", 0x0800);
                break;
            }

            #endregion
        }
    }

    private List<SkillTemplate> ObtainSkillList()
    {
        var skills = ServerSetup.Instance.GlobalSkillTemplateCache.Where(i => i.Value.NpcKey.ToLowerInvariant().Equals(Mundane.Template.Name.ToLowerInvariant())).ToArray();
        var possibleSkillTemplates = new List<SkillTemplate>();

        foreach (var (key, value) in skills)
        {
            possibleSkillTemplates.Add(value);
        }

        return possibleSkillTemplates;
    }

    private List<SpellTemplate> ObtainSpellList()
    {
        var spells = ServerSetup.Instance.GlobalSpellTemplateCache.Where(i => i.Value.NpcKey.ToLowerInvariant().Equals(Mundane.Template.Name.ToLowerInvariant())).ToArray();
        var possibleSpellTemplates = new List<SpellTemplate>();

        foreach (var (key, value) in spells)
        {
            possibleSpellTemplates.Add(value);
        }

        return possibleSpellTemplates;
    }
}