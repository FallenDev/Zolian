using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Naruto")]
public class EvermoreIllusionist : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public EvermoreIllusionist(WorldServer server, Mundane mundane) : base(server, mundane)
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

        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x01, "Why is Evermore unseen?")
        };

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.Ninja)
            options.Add(new(0x20, "Learn Ninja Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.Ninja)
            options.Add(new(0x30, "Learn Ninja Spells"));
        if (CanAdvance(client))
            options.Add(new(0x02, "Show me the way of the hidden path."));

        options.Add(new(0x00, "Leave."));

        var text = client.Aisling.QuestManager.EvermoreAssassinsSigilAttuned
            ? "Your sigil bends the lantern light around you. Hidden trails answer the marked."
            : "Don't believe everything you hear, and even less of what you see.";

        client.SendOptionsDialog(Mundane, text, [.. options]);
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
                    "Illusion magic and shadow contracts hold Evermore off every honest map. The sigil teaches the town to stop lying to you.");
                return;

            case 0x02:
                ExplainAdvancement(client);
                return;

            case 0x03:
                OnResponse(client, 0x999, $"{client.Aisling.Serial}");
                client.SendOptionsDialog(Mundane, "Then step through the fold. When you return, the hidden path will answer to the name Ninja.");
                return;

            case 0x999:
                AdvanceToNinja(client, args);
                return;

            case 0x20:
                EvermoreTrainingHelper.ShowSkillList(client, Mundane, _skillList, Job.Ninja);
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
                EvermoreTrainingHelper.LearnSkill(client, Mundane, args, "Move like the lantern flame never touched you.");
                return;

            case 0x30:
                EvermoreTrainingHelper.ShowSpellList(client, Mundane, _spellList, Job.Ninja);
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
                EvermoreTrainingHelper.LearnSpell(client, Mundane, args, "Let the veil remember your breath before it remembers your blade.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private bool CanAdvance(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        return client.Aisling.Stage == ClassStage.Master
               && client.Aisling.ExpLevel >= 275
               && quests.EvermoreAssassinsSigilAttuned
               && quests.EvermoreNinjaPathUnlocked
               && (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin);
    }

    private void ExplainAdvancement(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (!quests.EvermoreAssassinsSigilAttuned)
        {
            client.SendOptionsDialog(Mundane, "Kaelen has not attuned your sigil yet.");
            return;
        }

        if (!quests.EvermoreNinjaPathUnlocked)
        {
            client.SendOptionsDialog(Mundane, "Seris Vael has not released you to the hidden path. Finish the Blood Oath first.");
            return;
        }

        if (client.Aisling.ExpLevel < 275)
        {
            client.SendOptionsDialog(Mundane, "Your body still drags too much noise behind it. Return at level 250.");
            return;
        }

        if (client.Aisling.Path != Class.Assassin && client.Aisling.PastClass != Class.Assassin)
        {
            client.SendOptionsDialog(Mundane, "The hidden path branches from the assassin's art.");
            return;
        }

        client.SendOptionsDialog(Mundane,
            "The town already forgets you when you stand still. Shall I teach it your new name?",
            new Dialog.OptionsDataItem(0x03, "Let the veil take me."),
            new Dialog.OptionsDataItem(0x00, "Not yet."));
    }

    private void AdvanceToNinja(WorldClient client, string args)
    {
        if (!CanAdvance(client))
            return;

        var succeeded = uint.TryParse(args, out var serial);
        if (!succeeded || serial != client.Aisling.Serial)
            return;

        Skill.GiveTo(client.Aisling, "Amenotejikara");
        client.LoadSkillBook();
        var chakraStone = new Item();
        chakraStone = chakraStone.Create(client.Aisling, "Chakra Stone");
        chakraStone.GiveTo(client.Aisling);

        EvermoreTrainingHelper.AdvanceJob(client, Job.Ninja, "Ninja");
        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreNinja", LegendColor.TurquoiseG8, LegendIcon.Rogue, "Evermore: Walked the hidden path");
    }
}
