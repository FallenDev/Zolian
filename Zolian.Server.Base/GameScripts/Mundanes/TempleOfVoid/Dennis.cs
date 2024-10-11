using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.TempleOfVoid;

[Script("Dennis")]
public class Dennis : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Dennis(WorldServer server, Mundane mundane) : base(server, mundane)
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

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.SharpShooter)
            options.Add(new(0x20, "Learn Sharpshooter Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.SharpShooter)
            options.Add(new(0x30, "Learn Sharpshooter Spells"));

        if (client.Aisling.Path is Class.Cleric || client.Aisling.PastClass is Class.Cleric)
            if (client.Aisling.SkillBook.HasSkill("Blink") && !client.Aisling.HasItem("Cleric's Feather"))
                options.Add(new Dialog.OptionsDataItem(0x06, "Lost my Feather, help!"));
            else if (!client.Aisling.LegendBook.Has("Traversing the Divide (Blink)"))
                options.Add(new Dialog.OptionsDataItem(0x05, "Traversing the Divide"));

        if (client.Aisling.Stage <= ClassStage.Master
            && client.Aisling.QuestManager.AdventuresGuildReputation >= 6
            && client.Aisling.ExpLevel >= 450
            && (client.Aisling.Path == Class.Assassin
            || client.Aisling.PastClass == Class.Assassin
            || client.Aisling.Path == Class.Monk
            || client.Aisling.PastClass == Class.Monk
            || client.Aisling.Path == Class.Berserker
            || client.Aisling.PastClass == Class.Berserker
            || client.Aisling.Path == Class.Defender
            || client.Aisling.PastClass == Class.Defender))
        {
            options.Add(new(0x01, "Become a Spec Operations Sharpshooter"));
            client.SendOptionsDialog(Mundane, $"You know, I'm the task force leader for the Spec Ops unit. We could use able bodies like you.", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "Ah, did the guild send you?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                {
                    client.CloseDialog();
                }
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x02, "I pledge my oath to the Adventurer's Guild") };

                    client.SendOptionsDialog(Mundane, "You've already proven yourself to the guild. All that is left to do is swear an oath before me. \n" +
                                                      "Remember, once you do so. There is no going back, we're comrades for eternity.", options.ToArray());
                }
                break;
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x00, "*salutes*") };
                    OnResponse(client, 0x999, $"{client.Aisling.Serial}");
                    client.SendOptionsDialog(Mundane, "I will now perform the seal which binds. Congratulations Spec Operator - Sharpshooter, come back to me " +
                                                      "whenever you're ready to learn our advanced guild techniques.", options.ToArray());
                }
                break;
            case 0x999:
                {
                    var succeeded = uint.TryParse(args, out var serial);
                    if (!succeeded) return;
                    if (serial != client.Aisling.Serial) return;
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} has advanced to Special Operations - Sharpshooter"));
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(67, client.Aisling.Position));
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(116, false));
                    client.Aisling.Stage = ClassStage.Job;
                    client.Aisling.JobClass = Job.SharpShooter;

                    var legend = new Legend.LegendItem
                    {
                        Key = "LJob1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.TurquoiseG7,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Advanced to Job - Sharpshooter"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case 0x05:
                {
                    var skill = Skill.GiveTo(client.Aisling, "Blink");
                    if (skill) client.LoadSkillBook();
                    client.GiveItem("Cleric's Feather");
                    client.SendOptionsDialog(Mundane, "I can see you're worthy, here is something I created personally.");
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDennis hands you a feather");

                    var legend = new Legend.LegendItem
                    {
                        Key = "LDennis1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.PinkRedG5,
                        Icon = (byte)LegendIcon.Priest,
                        Text = "Traversing the Divide (Blink)"
                    };

                    if (!client.Aisling.LegendBook.Has("Traversing the Divide (Blink)"))
                        client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case 0x06:
                {
                    client.GiveItem("Cleric's Feather");
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDennis hands you a feather");
                    client.SendOptionsDialog(Mundane, "Here you go, try not to lose it again.");
                }
                break;
            case 0x20:
                {
                    ShowSkillList(client);
                }
                break;
            case 0x21:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x22, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x23, "Learn"),
                        new Dialog.OptionsDataItem(0x00, "No, thank you."));
                }
                break;
            case 0x22:
                {
                    ShowSkillDescription(client, args);
                }
                break;
            case 0x23:
                {
                    CheckSkillPrerequisites(client, args);
                }
                break;
            case 0x24:
                {
                    LearnSkill(client, args);
                }
                break;
            case 0x30:
                {
                    ShowSpellList(client);
                }
                break;
            case 0x31:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x32, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x33, "Learn"),
                        new Dialog.OptionsDataItem(0x00, "No, thank you."));
                }
                break;
            case 0x32:
                {
                    ShowSpellDescription(client, args);
                }
                break;
            case 0x33:
                {
                    CheckSpellPrerequisites(client, args);
                }
                break;
            case 0x34:
                {
                    LearnSpell(client, args);
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    #region Skills & Spells

    private void ShowSkillList(WorldClient client)
    {
        var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSkills = _skillList.Except(learnedSkills).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Samurai)).ToList();

        newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSkills.Count > 0)
        {
            client.SendSkillLearnDialog(Mundane, "What ability are we attempting? \nThese job abilities are unique to you.", 0x21, newSkills);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    private void CheckSkillPrerequisites(WorldClient client, string args)
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
            client.SendOptionsDialog(Mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(0x24, "Yes, Sir! *salutes*"),
                new Dialog.OptionsDataItem(0x00, "I will return"));
        }
    }

    private void ShowSkillDescription(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null) return;

        client.SendOptionsDialog(Mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(0x23, "Yes"),
            new Dialog.OptionsDataItem(0x00, "No"));
    }

    private void LearnSkill(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null) return;
        client.LearnSkill(Mundane, subject, "We go in, we get out! Mission accomplished!");
    }

    private void ShowSpellList(WorldClient client)
    {
        var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Samurai)).ToList();

        newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSpells.Count > 0)
        {
            client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x31, newSpells);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    private void CheckSpellPrerequisites(WorldClient client, string args)
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
            client.SendOptionsDialog(Mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(0x24, "Yes, Sir! *salutes*"),
                new Dialog.OptionsDataItem(0x00, "I will return"));
        }
    }

    private void ShowSpellDescription(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null) return;

        client.SendOptionsDialog(Mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(0x33, "Yes"),
            new Dialog.OptionsDataItem(0x00, "No"));
    }

    private void LearnSpell(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null) return;
        client.LearnSpell(Mundane, subject, "Spec Ops is not only apart of our name, it is who we are.");
    }

    #endregion
}