using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Scripting;

public abstract class MundaneScript : ObjectManager, IScriptBase
{
    protected long OnClickCheck;
    protected ushort CurrentResponseId;
    protected ushort LastResponseId;

    protected MundaneScript(GameServer server, Mundane mundane)
    {
        Server = server;
        Mundane = mundane;
    }

    protected Mundane Mundane { get; set; }
    protected GameServer Server { get; set; }

    public virtual void OnClick(GameServer server, GameClient client)
    {
        OnClickCheck = Random.Shared.NextInt64();
    }

    public virtual void OnGoldDropped(GameClient client, uint gold)
    {
        client.SendMessage(0x03, "What's this for? Thank you.");
    }

    public abstract void OnResponse(GameServer server, GameClient client, ushort responseId, string args);
    public abstract void TopMenu(IGameClient client);
    public virtual void OnGossip(GameServer server, GameClient client, string message) { }
    public virtual void TargetAcquired(Sprite target) { }
    public virtual void OnItemDropped(GameClient client, Item item) { }
}