using Darkages.Infrastructure;
using Darkages.Network.Formats.Models.ServerFormats;

namespace Darkages.Network.Components;

public class PingComponent : GameServerComponent
{
    public PingComponent(Server.GameServer server) : base(server)
    {
        Timer = new GameServerTimer(TimeSpan.FromMilliseconds(7000));
    }

    private GameServerTimer Timer { get; }

    private void Ping()
    {
        foreach (var client in Server.Clients.Values.Where(client => client != null))
        {
            client.Send(new ServerFormat3B());
            client.LastPing = DateTime.Now;
        }
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (Timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(Ping);
        Timer.Delay = elapsedTime + TimeSpan.FromMilliseconds(7000);
    }
}