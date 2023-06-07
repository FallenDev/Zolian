using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Shreek Warn")]
public class ShreekWarn : MundaneScript
{
    public ShreekWarn(GameServer server, Mundane mundane) : base(server, mundane) => Mundane = mundane;

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);
        client.SendOptionsDialog(Mundane, "What are you doing in my swamp? Alright, get out of here. All of you. Move it. Let's go.");
        client.SendMessage(0x03, "What are you doing in my swamp?");
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
        }
    }
}