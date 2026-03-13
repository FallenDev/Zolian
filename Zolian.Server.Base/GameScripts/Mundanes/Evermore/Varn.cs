using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Silent Blacksmith Varn")]
public class Varn : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Varn(WorldServer server, Mundane mundane) : base(server, mundane)
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

        if (client.Aisling.QuestManager.AssassinsGuildReputation < 3)
        {
            client.SendOptionsDialog(Mundane, "... (He ignores you. Your rank is too low.)");
            return;
        }

        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "Show me your blueprints.")
        };

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.DarkKnight)
            options.Add(new(0x20, "Learn Dark Knight Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.DarkKnight)
            options.Add(new(0x30, "Learn Dark Knight Spells"));
        if (CanAdvanceToDarkKnight(client))
            options.Add(new(0x02, "I seek the void-forged path."));

        options.Add(new(0x00, "Leave."));

        client.SendOptionsDialog(Mundane,
            "... (He lays out shadow-tempered daggers, assassin stars, void-touched polearms, and a blade blacker than the room around it.)",
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
                    "... (His finger taps a two-handed death knell blade, then a row of special daggers. The finest work requires assassin favor, thieves' silence, and a hand steady enough for the void.)");
                return;

            case 0x02:
                ExplainAdvancement(client);
                return;

            case 0x03:
                OnResponse(client, 0x999, $"{client.Aisling.Serial}");
                client.SendOptionsDialog(Mundane, "... (The forge light dies. When it returns, the void-forged path has accepted you.)");
                return;

            case 0x999:
                AdvanceToDarkKnight(client, args);
                return;

            case 0x20:
                EvermoreTrainingHelper.ShowSkillList(client, Mundane, _skillList, Job.DarkKnight);
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
                EvermoreTrainingHelper.LearnSkill(client, Mundane, args, "Carry the weight of the void, and let your enemies carry the fear.");
                return;

            case 0x30:
                EvermoreTrainingHelper.ShowSpellList(client, Mundane, _spellList, Job.DarkKnight);
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
                EvermoreTrainingHelper.LearnSpell(client, Mundane, args, "Even steel listens when the rift speaks through it.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private bool CanAdvanceToDarkKnight(WorldClient client)
    {
        return client.Aisling.Stage <= ClassStage.Master
               && client.Aisling.ExpLevel >= 250
               && client.Aisling.QuestManager.EvermoreDarkKnightPathUnlocked
               && client.Aisling.QuestManager.AssassinsGuildReputation >= 5
               && client.Aisling.QuestManager.ThievesGuildReputation >= 2
               && (client.Aisling.Path == Class.Berserker
                   || client.Aisling.PastClass == Class.Berserker
                   || client.Aisling.Path == Class.Assassin
                   || client.Aisling.PastClass == Class.Assassin);
    }

    private void ExplainAdvancement(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (!quests.EvermoreDarkKnightPathUnlocked || quests.AssassinsGuildReputation < 5)
        {
            client.SendOptionsDialog(Mundane, "... (He does not even look up. Only the Veilbound survive this forge.)");
            return;
        }

        if (quests.ThievesGuildReputation < 2)
        {
            client.SendOptionsDialog(Mundane, "... (He taps twice on the table. The basement has not vouched for you yet.)");
            return;
        }

        if (client.Aisling.ExpLevel < 250)
        {
            client.SendOptionsDialog(Mundane, "... (His stare says what his silence does not: return at level 250.)");
            return;
        }

        if (client.Aisling.Path != Class.Assassin
            && client.Aisling.PastClass != Class.Assassin
            && client.Aisling.Path != Class.Berserker
            && client.Aisling.PastClass != Class.Berserker)
        {
            client.SendOptionsDialog(Mundane, "... (The void-forged path favors berserkers and assassins. He turns the anvil away from you.)");
            return;
        }

        client.SendOptionsDialog(Mundane,
            "... (He offers the black blade hilt-first. Will you take the dark weight of it?)",
            new Dialog.OptionsDataItem(0x03, "I will bear it."),
            new Dialog.OptionsDataItem(0x00, "Not yet."));
    }

    private void AdvanceToDarkKnight(WorldClient client, string args)
    {
        if (!CanAdvanceToDarkKnight(client))
            return;

        var succeeded = uint.TryParse(args, out var serial);
        if (!succeeded || serial != client.Aisling.Serial)
            return;

        EvermoreTrainingHelper.AdvanceJob(client, Job.DarkKnight, "Dark Knight");
        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreDarkKnight", LegendColor.Red, LegendIcon.Warrior, "Evermore: Claimed the void-forged oath");
    }
}
