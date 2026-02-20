using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// Create logic for creating guild related items daggers, two-handed death knell, assassin stars, quivers, and polearms
[Script("Silent Blacksmith Varn")]
public class Varn(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);

        if (client.Aisling.QuestManager.AssassinsGuildReputation < 2)
        {
            client.SendOptionsDialog(Mundane, "... (He ignores you. Your rank is too low.)");
            return;
        }

        client.SendOptionsDialog(Mundane,
            "... (He nods and reveals blueprints for shadow-tempered daggers.)");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}