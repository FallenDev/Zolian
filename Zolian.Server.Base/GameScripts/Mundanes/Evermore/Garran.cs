using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Knight Garran")]
public class Garran(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_TOWN = 0x01;
    private const ushort R_CHOICE = 0x02;
    private const ushort R_VOID = 0x03;

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
            new(R_TOWN, "Why stay in Evermore?"),
            new(R_VOID, "What do you know of the rift?")
        };

        if (!string.IsNullOrWhiteSpace(client.Aisling.QuestManager.EvermoreArdynChoice))
            options.Add(new(R_CHOICE, "Ask about Ardyn Halvor."));

        options.Add(new(R_Leave, "That's all."));

        client.SendOptionsDialog(Mundane, "Assassins call it balance. I call it cowardice.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_TOWN:
                client.SendOptionsDialog(Mundane,
                    "I stay because someone ought to remember what justice sounds like when it is spoken out loud. Evermore prefers whispers.");
                return;

            case R_CHOICE:
                TalkAboutArdyn(client);
                return;

            case R_VOID:
                client.SendOptionsDialog(Mundane,
                    client.Aisling.QuestManager.EvermoreDarkKnightPathUnlocked
                        ? "The Dark Knight path draws on what festers below the town. If you touch that power, do it with both eyes open."
                        : "Whatever lies below this town is older than any guild charter. The assassins guard it. The thieves hunger for it.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }

    private void TalkAboutArdyn(WorldClient client)
    {
        var choice = client.Aisling.QuestManager.EvermoreArdynChoice;

        switch (choice)
        {
            case "Expose":
                client.SendOptionsDialog(Mundane, "Then there is still a little law left in you. Evermore needs more of that than it admits.");
                return;

            case "Kill":
                client.SendOptionsDialog(Mundane, "Convenient justice is the town's favorite lie. Do not let it become yours.");
                return;

            case "Recruit":
                client.SendOptionsDialog(Mundane, "You chose leverage over judgment. Maybe that keeps the peace. Maybe it only delays the rot.");
                return;

            default:
                client.SendOptionsDialog(Mundane, "I have nothing more to say on the polisher.");
                return;
        }
    }
}
