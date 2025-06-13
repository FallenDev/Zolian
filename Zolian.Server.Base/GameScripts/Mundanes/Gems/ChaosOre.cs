using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Gems;

[Script("ChaosOre")]
public class ChaosOre(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        client.SendOptionsDialog(Mundane, $"A deep twisted ore, staring into it sends you into a state of confusion forcing your gaze away.");
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args) { }
}