using Darkages.Infrastructure;
using Darkages.Network.Client;

namespace Darkages.Network.GameServer.Components;

public class MessageClearComponent : GameServerComponent
{
    public MessageClearComponent(Server.GameServer server) : base(server)
    {
        Timer = new GameServerTimer(TimeSpan.FromSeconds(ServerSetup.Instance.Config.MessageClearInterval));
    }

    private GameServerTimer Timer { get; set; }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (Timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(Message);
    }

    private void Message()
    {
        lock (Server.Clients)
        {
            foreach (var client in Server.Clients.Values.Where(Predicate).Where(Selector))
                client.SendMessage(0x01, "\u0000");
        }
    }

    private static bool Predicate(GameClient client)
    {
        return client?.Aisling != null;
    }

    private static bool Selector(GameClient client)
    {
        var readyTime = DateTime.Now;
        return (readyTime - client.LastMessageSent).TotalSeconds > 5;
    }
}