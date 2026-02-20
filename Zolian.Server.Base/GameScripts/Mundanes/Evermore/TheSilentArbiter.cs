using Darkages.Common;
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

        var options = new[]
        {
        new Dialog.OptionsDataItem(R_Briefing, "Explain the marked targets."),
        new Dialog.OptionsDataItem(R_Complete, "My marked targets are dead."),
        new Dialog.OptionsDataItem(R_Leave, "..."),
    };

        client.SendOptionsDialog(Mundane, "No witness. No mistake. No mercy.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane,
                    "A Blade Without Witness: Carry a Marking Scroll and eliminate 3 marked targets in level 350-400 maps. Kill no non-targets or the trial resets.");
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
        if (client.Aisling.QuestManager.AssassinsGuildReputation < 2)
        {
            client.SendOptionsDialog(Mundane, "You are not yet a Blade.");
            return;
        }

        if (!client.Aisling.HasKilled("Marked Target", 3))
        {
            client.SendOptionsDialog(Mundane, "Three marks. No excuses.");
            return;
        }

        if (client.Aisling.QuestManager.AssassinsGuildReputation < 3)
        {
            client.Aisling.QuestManager.AssassinsGuildReputation = 3;
            client.GiveItem("Shadow Cloak");
        }

        client.SendOptionsDialog(Mundane, "You are Shadow. The lower quarter now acknowledges your rank.");
    }
}
