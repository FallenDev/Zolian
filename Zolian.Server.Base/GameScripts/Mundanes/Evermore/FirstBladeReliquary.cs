using Darkages.Common;
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
        new Dialog.OptionsDataItem(R_NotYet, "Not yet."),
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
                client.SendOptionsDialog(Mundane,
                    "Veil of Eternity: Enter the Umbral Crypt (450-500), defeat The First Blade, and claim soulbound assassin relics.");
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
        if (client.Aisling.QuestManager.AssassinsGuildReputation < 4)
        {
            client.SendOptionsDialog(Mundane, "Become Veilbound before invoking the crypt.");
            return;
        }

        if (!client.Aisling.HasKilled("The First Blade", 1))
        {
            client.SendOptionsDialog(Mundane, "The spirit still stands. Finish the crypt.");
            return;
        }

        client.Aisling.QuestManager.AssassinsGuildReputation = 5;
        client.SendOptionsDialog(Mundane,
            "The veil accepts you. Final passives, guild teleports, and elite assassination contracts are unlocked.");
    }
}
