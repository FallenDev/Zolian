using Darkages.Common;
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
        new Dialog.OptionsDataItem(R_Briefing, "Give me the assignment."),
        new Dialog.OptionsDataItem(R_Complete, "I've completed your trial."),
        new Dialog.OptionsDataItem(R_NotNow, "Not now."),
    };

        client.SendOptionsDialog(Mundane, "Calm steps. Quiet breath. Speak quickly.", options);
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Briefing:
                client.SendOptionsDialog(Mundane, "Whispers in the Dark: Speak with 3 Evermore citizens who deny our guild, gather 5 Sealed Letters from Shadow Wolves or Masked Highwaymen, then return unseen by Guild Watchers.");
                return;

            case R_Complete:
                CompleteTrial(client);
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void CompleteTrial(WorldClient client)
    {
        if (client.Aisling.QuestManager.AssassinsGuildReputation >= 1)
        {
            client.SendOptionsDialog(Mundane, "You already bear the sigil. Move on.");
            return;
        }

        var wolvesDone = client.Aisling.HasKilled("Shadow Wolf", 5);
        var highwaymenDone = client.Aisling.HasKilled("Masked Highwayman", 5);

        if (!client.Aisling.HasStacks("Sealed Letter", 5) || (!wolvesDone && !highwaymenDone))
        {
            client.SendOptionsDialog(Mundane, "Not enough. Bring 5 letters and prove you've hunted in the dark.");
            return;
        }

        client.TakeAwayQuantity(client.Aisling, "Sealed Letter", 5);
        client.GiveItem("Assassin's Sigil");
        client.Aisling.QuestManager.AssassinsGuildReputation = 1;
        client.SendOptionsDialog(Mundane, "Good. The letters carry partial names. There is a leak inside Evermore.");
    }
}
