using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mehadi
{
    [Script("Shreek Warn")]
    public class ShreekWarn : MundaneScript
    {
        public ShreekWarn(GameServer server, Mundane mundane) : base(server, mundane) => Mundane = mundane;

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            client.SendOptionsDialog(Mundane, "What are you doing in my swamp? Alright, get out of here. All of you. Move it. Let's go.");
            client.SendMessage(0x03, "What are you doing in my swamp?");
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args) { }
    }
}
