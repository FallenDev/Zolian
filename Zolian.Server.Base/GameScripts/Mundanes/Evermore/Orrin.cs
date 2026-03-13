using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Keeper Orrin")]
public class Orrin(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Routes = 0x01;
    private const ushort R_Guild = 0x02;
    private const ushort R_TeleportSquare = 0x10;
    private const ushort R_TeleportHalls = 0x11;
    private const ushort R_ImperialCamp = 0x12;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var quests = client.Aisling.QuestManager;
        var options = new List<Dialog.OptionsDataItem>
        {
            new(R_Routes, "Tell me about your courier routes.")
        };

        if (quests.EvermoreWhispersStarted && !quests.EvermoreOrrinDeniedGuild)
            options.Add(new(R_Guild, "Do guild couriers use these routes?"));

        if (quests.AssassinsGuildReputation >= 2)
            options.Add(new(R_ImperialCamp, "I'd like passage to the Imperial Camp"));

        if (quests.EvermoreGuildTeleportUnlocked)
        {
            options.Add(new(R_TeleportSquare, "I'd like passage to Evermore"));
            options.Add(new(R_TeleportHalls, "Send me to the Umbral Descent"));
        }

        options.Add(new(R_Leave, "That's all."));

        client.SendOptionsDialog(Mundane, "Messages move faster when nobody can admit they carried them.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Routes:
                client.SendOptionsDialog(Mundane,
                    "My couriers map the hidden stairs, basements, and grave-silent halls of the deepest dungeons. Guildmaster's Chosen receive priority travel access.");
                return;

            case R_Guild:
                client.Aisling.QuestManager.EvermoreOrrinDeniedGuild = true;
                client.SendOptionsDialog(Mundane,
                    "Guild couriers? No. I move sealed parcels for paying clients. If some of those clients dress in shadow, it is not my profession to notice.");
                return;

            case R_TeleportSquare:
                client.TransitionToMap(289, new Position(55, 21));
                return;

            case R_TeleportHalls:
                client.TransitionToMap(289, new Position(39, 41));
                return;

            case R_ImperialCamp:
                client.TransitionToMap(12, new Position(39, 41));
                return;

            default:
                client.CloseDialog();
                return;
        }
    }
}
