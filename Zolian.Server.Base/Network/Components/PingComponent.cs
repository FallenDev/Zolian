using Darkages.Infrastructure;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PingComponent : WorldServerComponent
{
    public PingComponent(WorldServer server) : base(server)
    {
        Timer = new WorldServerTimer(TimeSpan.FromMilliseconds(7000));
    }

    private WorldServerTimer Timer { get; }

    private void Ping()
    {
        foreach (var player in Server.Aislings)
        {
            player.Client.SendHeartBeat(0x20, 0x14);
            player.Client.LastPing = DateTime.UtcNow;
        }
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (Timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(Ping);
        Timer.Delay = elapsedTime + TimeSpan.FromMilliseconds(7000);
    }
}