using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Shreek Warn")]
public class ShreekWarn : MundaneScript
{
    public ShreekWarn(WorldServer server, Mundane mundane) : base(server, mundane) => Mundane = mundane;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        client.SendOptionsDialog(Mundane, "What are you doing in my swamp? Alright, get out of here. All of you. Move it. Let's go.");
        client.SendServerMessage(ServerMessageType.ActiveMessage, "What are you doing in my swamp?");
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
        }
    }
}