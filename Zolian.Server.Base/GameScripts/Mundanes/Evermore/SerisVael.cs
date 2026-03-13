using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Seris Vael")]
public class SerisVael(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Briefing = 0x01;
    private const ushort R_Brew = 0x02;
    private const ushort R_Complete = 0x03;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        
        if (!client.Aisling.QuestManager.EvermoreAssassinsSigilAttuned)
        {
            client.SendOptionsDialog(Mundane, "I do not know you.");
            return;
        }

        if (client.Aisling.QuestManager.EvermoreBloodOathRewardClaimed)
        {
            client.SendOptionsDialog(Mundane, "You have already completed the blood oath.");
            return;
        }

        var options = new List<Dialog.OptionsDataItem>
        {
            new(R_Briefing, "Teach me the Blood Oath"),
            new(R_Brew, "Nightshade Venom?"),
            new(R_Complete, "It is done."),
            new(R_Leave, "Leave.")
        };

        client.SendOptionsDialog(Mundane, "Poison is only truth in liquid form.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane,
                    "Talk to Orrin for passage. I will be supplying you with a Nightshade Venom, you will enter the Imperial camp and test it on 15 Imperial Scouts and return alive.");
                return;

            case R_Brew:
                BrewNightshade(client);
                return;

            case R_Complete:
                CompleteTierTwo(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void BrewNightshade(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (!quests.EvermoreAssassinsSigilAttuned)
        {
            client.SendOptionsDialog(Mundane, "I do not know you.");
            return;
        }

        if (quests.EvermoreNightshadeVenomCrafted && client.Aisling.HasItem("Nightshade Venom"))
        {
            client.SendOptionsDialog(Mundane, "The vial already remembers your hand. Go test it on the Imperial Scouts.");
            return;
        }

        if (quests.EvermoreBloodOathRewardClaimed)
        {
            client.SendOptionsDialog(Mundane, "What's done is done.");
            return;
        }

        if (!client.TryGiveQuantity(client.Aisling, "Nightshade Venom", 20))
        {
            client.SendOptionsDialog(Mundane, "Your pack is too full to tuck away the Sealed Letter.");
            return;
        }

        quests.EvermoreNightshadeVenomCrafted = true;
        client.SendOptionsDialog(Mundane, "Good. Here is the venom. Now let it answer on 15 Imperial Scouts.");
    }

    private void CompleteTierTwo(WorldClient client)
    {
        var quests = client.Aisling.QuestManager;

        if (quests.AssassinsGuildReputation < 2)
        {
            client.SendOptionsDialog(Mundane, "Kaelen has not marked you yet.");
            return;
        }

        if (quests.EvermoreBloodOathRewardClaimed || quests.AssassinsGuildReputation >= 3)
        {
            client.SendOptionsDialog(Mundane, "You've already completed this oath.");
            return;
        }

        if (!quests.EvermoreNightshadeVenomCrafted)
        {
            client.SendOptionsDialog(Mundane, "You have not brewed the venom yet.");
            return;
        }

        if (!client.Aisling.LegendBook.HasQuantity("- Murdered Imperial", 15))
        {
            client.SendOptionsDialog(Mundane, "Your venom has not proven itself. Fifteen scouts. Seek Orrin.");
            return;
        }

        quests.AssassinsGuildReputation = 3;
        quests.EvermoreBloodOathRewardClaimed = true;
        quests.EvermoreNinjaPathUnlocked = true;
        client.Aisling._Dmg += 15;
        client.SendAttributes(StatUpdateType.Full);

        EvermoreQuestHelper.AddLegendIfMissing(client, "LEvermore2", LegendColor.TurquoiseG8, LegendIcon.Rogue,
            EvermoreQuestHelper.AssassinLegendRank(quests.AssassinsGuildReputation));

        client.SendOptionsDialog(Mundane, "Accepted. Your strikes bite deeper now, and the hidden path to the Ninja becomes visible to you.");
    }
}
