using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PingComponent(WorldServer server) : WorldServerComponent(server)
{
    private static void Ping()
    {
        foreach (var player in Server.Aislings)
        {
            player.Client.SendHeartBeat(0x20, 0x14);
        }
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(Ping);
    }
}