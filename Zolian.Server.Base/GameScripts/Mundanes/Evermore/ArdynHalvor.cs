using Darkages.Common;
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

        var options = new[]
        {
        new Dialog.OptionsDataItem(R_Briefing, "Confront Ardyn about the leak."),
        new Dialog.OptionsDataItem(R_Expose, "Expose Ardyn."),
        new Dialog.OptionsDataItem(R_Kill, "Kill Ardyn quietly."),
        new Dialog.OptionsDataItem(R_Recruit, "Recruit Ardyn."),
        new Dialog.OptionsDataItem(R_Leave, "Walk away."),
    };

        client.SendOptionsDialog(Mundane, "...polish, polish... signatures are easy to copy.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane,
                    "The Fractured Veil: Gather 10 Enchanted Residue, follow Ardyn at night, then choose to expose, kill, or recruit him.");
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
        if (client.Aisling.QuestManager.AssassinsGuildReputation < 3)
        {
            client.SendOptionsDialog(Mundane, "You are not authorized to decide this.");
            return;
        }

        if (!client.Aisling.HasStacks("Enchanted Residue", 10))
        {
            client.SendOptionsDialog(Mundane, "Bring 10 Enchanted Residue as proof.");
            return;
        }

        client.TakeAwayQuantity(client.Aisling, "Enchanted Residue", 10);
        client.Aisling.QuestManager.AssassinsGuildReputation = 4;

        switch (choice)
        {
            case R_Kill:
                client.SendOptionsDialog(Mundane, "No witness, no noise. Veilbound accepted. Dark Knight whispers begin.");
                break;
            case R_Recruit:
                client.SendOptionsDialog(Mundane, "Useful choice. Evermore's merchants and handlers will react to your restraint.");
                break;
            default:
                client.SendOptionsDialog(Mundane, "Public judgment. Streets grow louder, and shadows trust you less.");
                break;
        }
    }
}
