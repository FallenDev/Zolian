using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Kaelen Duskhand")]
public class KaelenDuskhand(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_NotNow = 0x00;
    private const ushort R_Briefing = 0x01;
    private const ushort R_Complete = 0x02;
    private const ushort R_Status = 0x03;

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
            new(R_Briefing, "Give me the assignment."),
            new(R_Complete, "I've completed your trial."),
            new(R_Status, "Measure my standing."),
            new(R_NotNow, "Not now.")
        };

        if (client.Aisling.QuestManager.EvermoreAssassinsSigilAttuned)
        {
            options.RemoveAll(x => x.Step == R_Briefing || x.Step == R_Complete || x.Step == R_Status);
        }

        if (!client.Aisling.QuestManager.EvermoreWhispersStarted)
        {
            options.RemoveAll(x => x.Step == R_Complete || x.Step == R_Status);
        }

        client.SendOptionsDialog(Mundane,
            $"Calm steps. Quiet breath. \n\n{{=bYour current standing with us: {{=c{EvermoreQuestHelper.AssassinRankName(client.Aisling.QuestManager.AssassinsGuildReputation)}",
            [.. options]);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                ShowBriefing(client);
                return;

            case R_Complete:
                CompleteTrial(client);
                return;

            case R_Status:
                ShowStatus(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void ShowBriefing(WorldClient client)
    {
        client.Aisling.QuestManager.EvermoreWhispersStarted = true;

        client.SendOptionsDialog(Mundane,
            $"{{=bWhispers in the Dark: {{=aAsk the Archivist, Candle Maker Yselle, and Keeper Orrin about the guild. Each will deny us. Bring me 2 Sealed Letters from the roads outside Evermore, and return with all three denials heard.");
    }

    private void ShowStatus(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;
        var denials = EvermoreQuestHelper.CountDeniedCitizens(quests);

        client.SendOptionsDialog(Mundane,
            $"Rank: {EvermoreQuestHelper.AssassinRankName(quests.AssassinsGuildReputation)}.\nCitizens denying us: {denials}/3.\nSigil attuned: {(quests.EvermoreAssassinsSigilAttuned ? "yes" : "no")}.");
    }

    private void CompleteTrial(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation >= 2)
        {
            client.SendOptionsDialog(Mundane, "You already bear the sigil. Move on.");
            return;
        }

        if (!quests.EvermoreWhispersStarted)
        {
            quests.EvermoreWhispersStarted = true;
            client.SendOptionsDialog(Mundane, "Hear the task before you claim it. Speak with the Archivist, Yselle, and Orrin first.");
            return;
        }

        if (!EvermoreQuestHelper.HasDeniedAllCitizens(quests))
        {
            client.SendOptionsDialog(Mundane,
                $"You have not heard enough lies yet. Three denials, no fewer. You have {EvermoreQuestHelper.CountDeniedCitizens(quests)}/3.");
            return;
        }

        if (!client.Aisling.HasStacks("Sealed Letter", 2))
        {
            client.SendOptionsDialog(Mundane, "Not enough. Bring 2 Sealed Letters and keep your hands clean.");
            return;
        }

        client.TakeAwayQuantity(client.Aisling, "Sealed Letter", 2);
        quests.EvermoreAssassinsSigilAttuned = true;
        quests.AssassinsGuildReputation = 2;

        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermore1", LegendColor.TurquoiseG7, LegendIcon.Rogue,
            EvermoreQuestHelper.AssassinLegendRank(quests.AssassinsGuildReputation));

        client.SendOptionsDialog(Mundane, "Good. The letters carry partial names. There is a leak inside Evermore, and now the sigil behind Rionnag will answer to you.");
    }
}
