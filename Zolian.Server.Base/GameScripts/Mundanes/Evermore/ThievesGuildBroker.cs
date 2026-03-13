using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Thieves Guild Broker")]
public class ThievesGuildBroker : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public ThievesGuildBroker(WorldServer server, Mundane mundane) : base(server, mundane)
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

        var quests = client.Aisling.QuestManager;
        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "How does your guild work?"),
            new(0x02, "Measure my standing.")
        };

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.Thief)
            options.Add(new(0x20, "Learn Thief Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.Thief)
            options.Add(new(0x30, "Learn Thief Spells"));
        if (CanAdvanceToThief(client))
            options.Add(new(0x03, "I want into the trade."));

        options.Add(new(0x00, "That's all."));

        client.SendOptionsDialog(Mundane,
            $"Less honor. More profit. Your current standing is {EvermoreQuestHelper.ThievesRankName(quests.ThievesGuildReputation)}.",
            options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case 0x00:
                client.CloseDialog();
                return;

            case 0x01:
                client.SendOptionsDialog(Mundane,
                    "We trade in routes, leverage, black market access, and doors that were supposed to stay closed. Thieves thrive when both guilds need them.");
                return;

            case 0x02:
                AdvanceStanding(client);
                return;

            case 0x03:
                ExplainAdvancement(client);
                return;

            case 0x04:
                OnResponse(client, 0x999, $"{client.Aisling.Serial}");
                client.SendOptionsDialog(Mundane, "Then welcome to the trade. Keep your hands quicker than your conscience.");
                return;

            case 0x999:
                AdvanceToThief(client, args);
                return;

            case 0x20:
                EvermoreTrainingHelper.ShowSkillList(client, Mundane, _skillList, Job.Thief);
                return;

            case 0x21:
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "?",
                    args,
                    new Dialog.OptionsDataItem(0x22, $"What does {args} do?"),
                    new Dialog.OptionsDataItem(0x23, "Learn"),
                    new Dialog.OptionsDataItem(0x00, "Not now."));
                return;

            case 0x22:
                EvermoreTrainingHelper.ShowSkillDescription(client, Mundane, args, 0x23);
                return;

            case 0x23:
                EvermoreTrainingHelper.CheckSkillPrerequisites(client, Mundane, args, 0x24);
                return;

            case 0x24:
                EvermoreTrainingHelper.LearnSkill(client, Mundane, args, "Steal time first. Coin follows.");
                return;

            case 0x30:
                EvermoreTrainingHelper.ShowSpellList(client, Mundane, _spellList, Job.Thief);
                return;

            case 0x31:
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "?",
                    args,
                    new Dialog.OptionsDataItem(0x32, $"What does {args} do?"),
                    new Dialog.OptionsDataItem(0x33, "Learn"),
                    new Dialog.OptionsDataItem(0x00, "Not now."));
                return;

            case 0x32:
                EvermoreTrainingHelper.ShowSpellDescription(client, Mundane, args, 0x33);
                return;

            case 0x33:
                EvermoreTrainingHelper.CheckSpellPrerequisites(client, Mundane, args, 0x34);
                return;

            case 0x34:
                EvermoreTrainingHelper.LearnSpell(client, Mundane, args, "A good secret is lighter than steel and twice as sharp.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private void AdvanceStanding(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation < 4)
        {
            client.SendOptionsDialog(Mundane, "Come back after the Silent Arbiter knows your work. We do not sponsor strangers.");
            return;
        }

        if (quests.ThievesGuildReputation < 1)
        {
            quests.ThievesGuildReputation = 1;
            EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreThief1", LegendColor.WhiteBlackG4, LegendIcon.Rogue,
                EvermoreQuestHelper.ThievesLegendRank(quests.ThievesGuildReputation));
            client.SendOptionsDialog(Mundane, "The marked trial made noise in the right cellar. You're a Lookout now. Keep listening.");
            return;
        }

        if (quests.ThievesGuildReputation < 2)
        {
            if (quests.AssassinsGuildReputation < 5 || string.IsNullOrWhiteSpace(quests.EvermoreArdynChoice))
            {
                client.SendOptionsDialog(Mundane, "Shadow standing comes after Ardyn's ledger closes. Settle the fractured veil first.");
                return;
            }

            quests.ThievesGuildReputation = 2;
            quests.EvermoreThiefPathUnlocked = true;
            EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreThief2", LegendColor.TurquoiseG7, LegendIcon.Rogue,
                EvermoreQuestHelper.ThievesLegendRank(quests.ThievesGuildReputation));
            client.SendOptionsDialog(Mundane, "Ardyn's fate proved you're useful. You now carry Shadow standing, and the Thief path is open to you.");
            return;
        }

        if (quests.ThievesGuildReputation < 3)
        {
            if (!quests.EvermoreFirstBladeRewardClaimed)
            {
                client.SendOptionsDialog(Mundane, "Night Broker standing waits beyond the Umbral Crypt. Bring back the First Blade's silence.");
                return;
            }

            quests.ThievesGuildReputation = 3;
            EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreThief3", LegendColor.WhiteBlackG16, LegendIcon.Rogue,
                EvermoreQuestHelper.ThievesLegendRank(quests.ThievesGuildReputation));
            client.SendOptionsDialog(Mundane, "The Reliquary changed the market overnight. You are Night Broker now. Every back door in Evermore knows your tread.");
            return;
        }

        client.SendOptionsDialog(Mundane,
            $"You already hold {EvermoreQuestHelper.ThievesRankName(quests.ThievesGuildReputation)} standing with us.");
    }

    private bool CanAdvanceToThief(WorldClient client)
    {
        return client.Aisling.Stage <= ClassStage.Master
               && client.Aisling.ExpLevel >= 250
               && client.Aisling.QuestManager.EvermoreThiefPathUnlocked
               && (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin);
    }

    private void ExplainAdvancement(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (!quests.EvermoreThiefPathUnlocked)
        {
            client.SendOptionsDialog(Mundane, "Earn Shadow standing with us first. We do not open the ledger for tourists.");
            return;
        }

        if (client.Aisling.ExpLevel < 250)
        {
            client.SendOptionsDialog(Mundane, "Come back at level 250. A slow hand gets cut.");
            return;
        }

        if (client.Aisling.Path != Class.Assassin && client.Aisling.PastClass != Class.Assassin)
        {
            client.SendOptionsDialog(Mundane, "Our trade favors hands already shaped by the assassin's path.");
            return;
        }

        client.SendOptionsDialog(Mundane,
            "Ready to trade clean titles for useful ones?",
            new Dialog.OptionsDataItem(0x04, "Open the ledger."),
            new Dialog.OptionsDataItem(0x00, "Not yet."));
    }

    private void AdvanceToThief(WorldClient client, string args)
    {
        if (!CanAdvanceToThief(client))
            return;

        var succeeded = uint.TryParse(args, out var serial);
        if (!succeeded || serial != client.Aisling.Serial)
            return;

        EvermoreTrainingHelper.AdvanceJob(client, Job.Thief, "Thief");
        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreThiefJob", LegendColor.WhiteBlackG8, LegendIcon.Rogue, "Evermore: Took the thief's ledger oath");
    }
}
