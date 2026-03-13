using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Candle Maker Yselle")]
public class Yselle(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private const ushort R_Leave = 0x00;
    private const ushort R_Candles = 0x01;
    private const ushort R_Guild = 0x02;
    private const ushort R_Blend = 0x03;

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
            new(R_Candles, "What are shadow-light candles?"),
            new(R_Blend, "What is your advanced blend?")
        };

        if (quests.EvermoreWhispersStarted && !quests.EvermoreYselleDeniedGuild)
            options.Add(new(R_Guild, "Do assassins buy from you?"));

        options.Add(new(R_Leave, "Leave."));

        client.SendOptionsDialog(Mundane, "Lantern wax tells the truth that faces won't.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client))
            return;

        switch (responseId)
        {
            case R_Candles:
                client.SendOptionsDialog(Mundane,
                    "Shadow-light candles bend lamp glow inward instead of outward. They make alleys quieter, windows dimmer, and nervous people forget they saw you.");
                return;

            case R_Guild:
                client.Aisling.QuestManager.EvermoreYselleDeniedGuild = true;
                client.SendOptionsDialog(Mundane,
                    "Assassins? I sell candles, not confessions. If one of them shops here, they pay in exact coin and speak even less than you do.");
                return;

            case R_Blend:
                client.SendOptionsDialog(Mundane,
                    "My advanced blend feeds illusion wards and poison work. Seris Vael buys more of it than she admits.");
                return;

            default:
                client.CloseDialog();
                return;
        }
    }
}
