using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Evermore Archivist")]
public class EvermoreArchivist(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Lore = 0x01;
    private const ushort R_Guild = 0x02;
    private const ushort R_Rift = 0x03;

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
            new(R_Lore, "Tell me about Evermore."),
            new(R_Rift, "What lies beneath the town?")
        };

        if (quests.EvermoreWhispersStarted && !quests.EvermoreArchivistDeniedGuild)
            options.Add(new(R_Guild, "Have you heard of the Assassin's Guild?"));

        options.Add(new(R_Leave, "That's all."));

        client.SendOptionsDialog(Mundane, "Records outlived kingdoms. Secrets only pretend to.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Lore:
                client.SendOptionsDialog(Mundane,
                    "Evermore was founded by exiled royal executioners. They built a town that looks restful from a distance and impossible to read from within.");
                return;

            case R_Guild:
                client.Aisling.QuestManager.EvermoreArchivistDeniedGuild = true;
                client.SendOptionsDialog(Mundane,
                    "Assassins? I maintain ledgers, not murder clubs. If you are chasing tavern stories, ask someone who enjoys candle smoke more than parchment.");
                return;

            case R_Rift:
                client.SendOptionsDialog(Mundane,
                    "The Guild protects the Umbral Crypt beneath these streets. Others would rather inventory it, fence it, and profit from every whisper it emits.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }
}
