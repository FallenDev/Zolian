using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// Buy "advanced blend" item
[Script("Candle Maker Yselle")]
public class Yselle(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.SendOptionsDialog(Mundane,
            "Shadow-light candles keep assassins unseen. Bring {=qchaos ore{=a and I'll improve my stock.");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}
