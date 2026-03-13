using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("First Blade Reliquary")]
public class FirstBladeReliquary(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_NotYet = 0x00;
    private const ushort R_Briefing = 0x01;
    private const ushort R_Complete = 0x02;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new[]
        {
            new Dialog.OptionsDataItem(R_Briefing, "Tell me of the Umbral Crypt."),
            new Dialog.OptionsDataItem(R_Complete, "The First Blade has fallen."),
            new Dialog.OptionsDataItem(R_NotYet, "Not yet.")
        };

        client.SendOptionsDialog(Mundane, "Only Veilbound may descend. Only the chosen return whole.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.Aisling.QuestManager.EvermoreVeilOfEternityStarted = true;
                client.SendOptionsDialog(Mundane,
                    "Veil of Eternity: Descend into the Umbral Crypt, break the ancient assassin spirit called the First Blade, and return to claim the seal of Guildmaster's Chosen.");
                return;

            case R_Complete:
                CompleteTierFive(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void CompleteTierFive(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation < 5)
        {
            client.SendOptionsDialog(Mundane, "Become Veilbound before invoking the crypt.");
            return;
        }

        if (quests.EvermoreFirstBladeRewardClaimed || quests.AssassinsGuildReputation >= 6)
        {
            client.SendOptionsDialog(Mundane, "The reliquary has already accepted your name.");
            return;
        }

        if (!client.Aisling.HasKilled("The First Blade", 1))
        {
            client.SendOptionsDialog(Mundane, "The spirit still stands. Finish the crypt.");
            return;
        }

        quests.AssassinsGuildReputation = 6;
        quests.EvermoreFirstBladeRewardClaimed = true;
        quests.EvermoreGuildTeleportUnlocked = true;
        client.Aisling._Dmg += 5;
        client.Aisling._Hit += 5;
        client.SendAttributes(StatUpdateType.Full);

        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermore5", LegendColor.TurquoiseG8, LegendIcon.Victory,
            EvermoreQuestHelper.AssassinLegendRank(quests.AssassinsGuildReputation));
        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermoreCrypt", LegendColor.WhiteBlackG16, LegendIcon.Victory, "Evermore: Endured the First Blade");

        client.SendOptionsDialog(Mundane,
            "The veil accepts you. Your hand strikes truer, the guild's couriers answer your summons, and Evermore now counts you among the Chosen.");
    }
}
