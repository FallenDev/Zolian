using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Ardyn Halvor")]
public class ArdynHalvor(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Briefing = 0x01;
    private const ushort R_Expose = 0x10;
    private const ushort R_Kill = 0x11;
    private const ushort R_Recruit = 0x12;

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
            new(R_Briefing, "Confront Ardyn about the leak.")
        };

        if (client.Aisling.QuestManager.AssassinsGuildReputation >= 4
            && string.IsNullOrWhiteSpace(client.Aisling.QuestManager.EvermoreArdynChoice))
        {
            options.Add(new(R_Expose, "Expose Ardyn."));
            options.Add(new(R_Kill, "Kill Ardyn quietly."));
            options.Add(new(R_Recruit, "Recruit Ardyn."));
        }

        options.Add(new(R_Leave, "Walk away."));

        client.SendOptionsDialog(Mundane, "...polish, polish... signatures are easy to copy.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane,
                    "The Fractured Veil: Gather 10 Enchanted Residue, shadow Ardyn through the night market, then decide whether his usefulness is worth the rot he caused.");
                return;

            case R_Expose:
            case R_Kill:
            case R_Recruit:
                ResolveTierFour(client, responseId);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void ResolveTierFour(WorldClient client, ushort choice)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation < 4)
        {
            client.SendOptionsDialog(Mundane, "You are not authorized to decide this.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(quests.EvermoreArdynChoice))
        {
            client.SendOptionsDialog(Mundane, $"Judgment has already been passed: {quests.EvermoreArdynChoice}.");
            return;
        }

        if (!client.Aisling.HasStacks("Enchanted Residue", 10))
        {
            client.SendOptionsDialog(Mundane, "Bring 10 Enchanted Residue as proof.");
            return;
        }

        client.TakeAwayQuantity(client.Aisling, "Enchanted Residue", 10);
        quests.AssassinsGuildReputation = 5;
        quests.EvermoreDarkKnightPathUnlocked = true;

        string choiceName;
        string responseText;
        string legendText;
        LegendColor legendColor;

        switch (choice)
        {
            case R_Kill:
                choiceName = "Kill";
                responseText = "No witness, no noise. Veilbound accepted. Something older than the guild has noticed you.";
                legendText = "Evermore: Buried Ardyn beneath the hush";
                legendColor = LegendColor.Red;
                break;
            case R_Recruit:
                choiceName = "Recruit";
                responseText = "Useful choice. Evermore's merchants and handlers will remember that you value leverage over panic.";
                legendText = "Evermore: Bound Ardyn to the guild's silence";
                legendColor = LegendColor.WhiteBlackG8;
                break;
            default:
                choiceName = "Expose";
                responseText = "Public judgment. Streets grow louder, but even the clean-handed can cast a long shadow.";
                legendText = "Evermore: Lit Ardyn's name before the lantern court";
                legendColor = LegendColor.Yellow;
                break;
        }

        quests.EvermoreArdynChoice = choiceName;

        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermore4", LegendColor.TurquoiseG8, LegendIcon.Rogue,
            EvermoreQuestHelper.AssassinLegendRank(quests.AssassinsGuildReputation));
        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreArdyn", legendColor, LegendIcon.Rogue, legendText);

        client.SendOptionsDialog(Mundane, responseText);
    }
}
