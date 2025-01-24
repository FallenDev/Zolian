using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Nobis;

[Script("Vivi")]
public class Vivi : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Vivi(WorldServer server, Mundane mundane) : base(server, mundane)
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

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.Oracle)
            options.Add(new(0x20, "Learn Oracle Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.Oracle)
            options.Add(new(0x30, "Learn Oracle Spells"));


        if (client.Aisling.Stage <= ClassStage.Master
            && client.Aisling.ExpLevel >= 250
            && (client.Aisling.Path == Class.Cleric
                 || client.Aisling.PastClass == Class.Cleric
                 || client.Aisling.Path == Class.Arcanus
                 || client.Aisling.PastClass == Class.Arcanus))
        {
            options.Add(new(0x01, "Inquire about becoming an Oracle"));
            client.SendOptionsDialog(Mundane, $"You want to learn what I know? Perhaps that will help me remember...", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "Where am I?!", options.ToArray());
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
                    var options = new List<Dialog.OptionsDataItem> { new(0x02, "I will Vivi") };

                    client.SendOptionsDialog(Mundane, "These abilities of mine, they're powerful.. you have to be certain to use them only for good.", options.ToArray());
                }
                break;
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x00, "*nod*") };
                    OnResponse(client, 0x999, $"{client.Aisling.Serial}");
                    client.SendOptionsDialog(Mundane, "Alright, *holds hands up* I'm now attaching a part of my power onto you.", options.ToArray());
                }
                break;
            case 0x999:
                {
                    var succeeded = uint.TryParse(args, out var serial);
                    if (!succeeded) return;
                    if (serial != client.Aisling.Serial) return;
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} has advanced to Oracle"));
                    client.Aisling.SendAnimationNearby(67, client.Aisling.Position);
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(116, false));
                    client.Aisling.Stage = ClassStage.Job;
                    client.Aisling.JobClass = Job.Oracle;

                    var legend = new Legend.LegendItem
                    {
                        Key = "LJob1",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.TurquoiseG7,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Advanced to Job - Oracle"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
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
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Oracle)).ToList();

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
                new Dialog.OptionsDataItem(0x24, "Yes, Vivi, let's proceed"),
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
        client.LearnSkill(Mundane, subject, "Perhaps know, we'll learn something?");
    }

    private void ShowSpellList(WorldClient client)
    {
        var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Oracle)).ToList();

        newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSpells.Count > 0)
        {
            client.SendSpellLearnDialog(Mundane, "Here are the secrets I have to teach.", 0x31, newSpells);
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
                new Dialog.OptionsDataItem(0x34, "Yes, Vivi let's proceed!"),
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