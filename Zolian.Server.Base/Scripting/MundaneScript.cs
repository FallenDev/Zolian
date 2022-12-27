using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Scripting;

public abstract class MundaneScript : ObjectManager, IScriptBase
{
    protected MundaneScript(GameServer server, Mundane mundane)
    {
        Server = server;
        Mundane = mundane;
    }

    protected Mundane Mundane { get; set; }
    protected GameServer Server { get; set; }

    public abstract void OnClick(GameServer server, GameClient client);
    public abstract void TopMenu(IGameClient client);
    public abstract void OnResponse(GameServer server, GameClient client, ushort responseId, string args);
    public virtual void OnGossip(GameServer server, GameClient client, string message) { }
    public virtual void TargetAcquired(Sprite target) { }
    public virtual void OnItemDropped(GameClient client, Item item) { }

    public virtual void OnGoldDropped(GameClient client, uint gold)
    {
        client.SendMessage(0x03, "What's this for? Thank you.");
    }
}