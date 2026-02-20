using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Thieves Guild Broker")]
public class ThievesGuildBroker(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Info = 0x01;
    private const ushort R_Reputation = 0x02;
    private const ushort R_DarkKnightCheck = 0x03;

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
        new Dialog.OptionsDataItem(R_Info, "How does your guild work?"),
        new Dialog.OptionsDataItem(R_Reputation, "Increase my standing."),
        new Dialog.OptionsDataItem(R_DarkKnightCheck, "Can I pursue being a Thief?"),
        new Dialog.OptionsDataItem(R_Leave, "That's all"),
    };

        client.SendOptionsDialog(Mundane, "Less honor. More profit.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Info:
                client.SendOptionsDialog(Mundane,
                    "Reputation unlocks pickpocket scaling, black market vendors, smuggled gear, and lock bypass mechanics.");
                return;

            case R_Reputation:
                client.Aisling.QuestManager.ThievesGuildReputation++;
                client.SendOptionsDialog(Mundane, "Good. Keep proving your value.");
                return;

            case R_DarkKnightCheck:
                var ready = client.Aisling.QuestManager.AssassinsGuildReputation >= 4
                            && client.Aisling.QuestManager.ThievesGuildReputation >= 1;
                client.SendOptionsDialog(Mundane,
                    ready
                        ? "You qualify. Seek the void-touched masters beneath Evermore."
                        : "Not yet. You need standing in both guilds and completion of the Tier 4 decision.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }
}
