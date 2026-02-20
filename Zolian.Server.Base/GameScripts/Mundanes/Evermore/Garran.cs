using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Evermore;

// Adventuers Guild holdout quest 
[Script("Knight Garran")]
public class Garran(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.SendOptionsDialog(Mundane,
            "Assassins call it balance. I call it cowardice. Return when you're ready to face that truth.");
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args) => client.CloseDialog();
}
