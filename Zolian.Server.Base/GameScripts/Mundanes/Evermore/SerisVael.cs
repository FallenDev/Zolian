using Darkages.Common;
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
        new Dialog.OptionsDataItem(R_Briefing, "Teach me the Blood Oath."),
        new Dialog.OptionsDataItem(R_Complete, "I've brewed and tested the venom."),
        new Dialog.OptionsDataItem(R_Leave, "Leave."),
    };

        client.SendOptionsDialog(Mundane, "Poison is only truth in liquid form.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane,
                    "Blood Oath: Craft Nightshade Venom with 3 Dusk Petals and 1 Vial of Corrupted Blood, then test it on 15 Imperial Scouts and return alive.");
                return;

            case R_Complete:
                CompleteTierTwo(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void CompleteTierTwo(WorldClient client)
    {
        if (client.Aisling.QuestManager.AssassinsGuildReputation < 1)
        {
            client.SendOptionsDialog(Mundane, "Kaelen has not marked you yet.");
            return;
        }

        if (client.Aisling.QuestManager.AssassinsGuildReputation >= 2)
        {
            client.SendOptionsDialog(Mundane, "You've already completed this oath.");
            return;
        }

        if (!client.Aisling.HasKilled("Imperial Scout", 15))
        {
            client.SendOptionsDialog(Mundane, "Your venom has not proven itself. Fifteen scouts.");
            return;
        }

        client.Aisling.QuestManager.AssassinsGuildReputation = 2;
        client.SendOptionsDialog(Mundane, "Accepted. Your backstab grows stronger and Shadow Training opens.");
    }
}
