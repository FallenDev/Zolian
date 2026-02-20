using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// Buy/Sell Guild Scrolls to Evermore if Rank 3+
[Script("Keeper Orrin")]
public class Orrin(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.SendOptionsDialog(Mundane,
            "My couriers run routes on map records. Guildmaster's Chosen receive priority travel access.");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}
