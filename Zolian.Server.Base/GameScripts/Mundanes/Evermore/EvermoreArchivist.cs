using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// Introduce Void Rift dungeon (Prestigue Rank 1 and higher rifting)
[Script("Evermore Archivist")]
public class EvermoreArchivist(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.SendOptionsDialog(Mundane,
            "Evermore was founded by exiled royal executioners. The Assassin's Guild protects the sealed void rift beneath us.");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}
