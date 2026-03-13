using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("The Silent Arbiter")]
public class SilentArbiter(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
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

        if (!client.Aisling.QuestManager.EvermoreBloodOathRewardClaimed)
        {
            client.SendOptionsDialog(Mundane, "Talk to Seris Vael.");
            return;
        }

        var options = new List<Dialog.OptionsDataItem>
        {
            new (R_Briefing, "Explain the marked targets."),
            new (R_Complete, "My marked targets are dead."),
            new (R_Leave, "...")
        };

        if (client.Aisling.QuestManager.EvermoreShadowCloakClaimed)
        {
            options.RemoveAll(x => x.Step == R_Briefing || x.Step == R_Complete);
        }

        if (client.Aisling.QuestManager.EvermoreShadowCloakClaimed)
        {
            client.SendOptionsDialog(Mundane, "You are Shadow. The lower quarter now acknowledges your rank, and the thieves have begun to listen when your name is spoken.", [.. options]);
            return;
        }

        client.SendOptionsDialog(Mundane, "No witness. No mistake. No mercy.", [.. options]);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                if (!client.TryGiveQuantity(client.Aisling, "Nightshade Venom", 3))
                {
                    client.SendOptionsDialog(Mundane, "Your pack is too full to tuck away the Sealed Letter.");
                    return;
                }

                client.Aisling.QuestManager.EvermoreMarkedTrialStarted = true;
                client.SendOptionsDialog(Mundane, $"Carry Nightshade and eliminate Neal, Dar, and Dredrick. They won't know what hit them.");
                return;

            case R_Complete:
                CompleteTierThree(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void CompleteTierThree(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation < 3)
        {
            client.SendOptionsDialog(Mundane, "You are not yet a Blade.");
            return;
        }

        if (quests.EvermoreShadowCloakClaimed || quests.AssassinsGuildReputation >= 4)
        {
            client.SendOptionsDialog(Mundane, "You already carry the silence of Shadow.");
            return;
        }

        if (!client.Aisling.LegendBook.HasQuantity("- Murdered Dar", 1) ||
            !client.Aisling.LegendBook.HasQuantity("- Murdered Neal", 1) ||
            !client.Aisling.LegendBook.HasQuantity("- Murdered Dredrick", 1))
        {
            client.SendOptionsDialog(Mundane, "Three marks. No excuses.");
            return;
        }

        quests.AssassinsGuildReputation = 4;
        quests.EvermoreShadowCloakClaimed = true;
        var armor = new Item();
        armor = armor.Create(client.Aisling, "Shadow Reaper");
        armor.GiveTo(client.Aisling);

        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermore3", LegendColor.BluePurpleG3, LegendIcon.Rogue,
            EvermoreQuestHelper.AssassinLegendRank(quests.AssassinsGuildReputation));

        client.SendOptionsDialog(Mundane, "You are Shadow. The lower quarter now acknowledges your rank, and the thieves have begun to listen when your name is spoken.");
    }
}
