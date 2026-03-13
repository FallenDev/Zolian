using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

internal static class EvermoreTrainingHelper
{
    public static void AdvanceJob(WorldClient client, Job job, string jobName)
    {
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
            c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} has advanced to {jobName}"));
        client.Aisling.SendAnimationNearby(67, client.Aisling.Position);
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(116, false));
        client.Aisling.Stage = ClassStage.Job;
        client.Aisling.JobClass = job;

        var legend = new Legend.LegendItem
        {
            Key = "LJob1",
            IsPublic = true,
            Time = DateTime.UtcNow,
            Color = LegendColor.YellowG7,
            Icon = (byte)LegendIcon.Victory,
            Text = $"Advanced to Job - {jobName}"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }

    public static void ShowSkillList(WorldClient client, Mundane mundane, List<SkillTemplate> skillList, Job job)
    {
        var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSkills = skillList.Except(learnedSkills).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(job)).ToList();

        newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSkills.Count > 0)
        {
            client.SendSkillLearnDialog(mundane, "What technique are you seeking? These teachings belong to your hidden path.", 0x21, newSkills);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    public static void CheckSkillPrerequisites(WorldClient client, Mundane mundane, string args, ushort responseId)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null)
            return;

        var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
        {
            if (!result)
                client.SendOptionsDialog(mundane, msg, subject.Name);
        });

        if (conditions)
        {
            client.SendOptionsDialog(mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(responseId, "Yes."),
                new Dialog.OptionsDataItem(0x00, "Not yet."));
        }
    }

    public static void ShowSkillDescription(WorldClient client, Mundane mundane, string args, ushort responseId)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null)
            return;

        client.SendOptionsDialog(mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(responseId, "Yes."),
            new Dialog.OptionsDataItem(0x00, "No."));
    }

    public static void LearnSkill(WorldClient client, Mundane mundane, string args, string learnMessage)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null)
            return;

        client.LearnSkill(mundane, subject, learnMessage);
    }

    public static void ShowSpellList(WorldClient client, Mundane mundane, List<SpellTemplate> spellList, Job job)
    {
        var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSpells = spellList.Except(learnedSpells).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(job)).ToList();

        newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSpells.Count > 0)
        {
            client.SendSpellLearnDialog(mundane, "Which secret are you ready to carry? The veil only shares what you can bear.", 0x31, newSpells);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    public static void CheckSpellPrerequisites(WorldClient client, Mundane mundane, string args, ushort responseId)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null)
            return;

        var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
        {
            if (!result)
                client.SendOptionsDialog(mundane, msg, subject.Name);
        });

        if (conditions)
        {
            client.SendOptionsDialog(mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(responseId, "Yes."),
                new Dialog.OptionsDataItem(0x00, "Not yet."));
        }
    }

    public static void ShowSpellDescription(WorldClient client, Mundane mundane, string args, ushort responseId)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null)
            return;

        client.SendOptionsDialog(mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(responseId, "Yes."),
            new Dialog.OptionsDataItem(0x00, "No."));
    }

    public static void LearnSpell(WorldClient client, Mundane mundane, string args, string learnMessage)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null)
            return;

        client.LearnSpell(mundane, subject, learnMessage);
    }
}
