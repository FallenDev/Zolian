using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// If onApproach you're wearing a sigil of the guild a portal animation will show where you can enter to learn "Ninja" Job class
[Script("Evermore Illusionist")]
public class EvermoreIllusionist(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.SendOptionsDialog(Mundane,
            "I veil this town from hostile eyes. Carry your sigil, and hidden trails become visible.");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}
