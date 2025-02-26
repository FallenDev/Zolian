using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites;

namespace Darkages.Network.Components;

public class PingComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 7000;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(50);
                continue;
            }

            var players = Server.Aislings;
            players.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(Ping);

            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static void Ping(Aisling player)
    {
        if (player?.Client == null) return;
        if (!player.LoggedIn) return;
        player.Client.SendHeartBeat(0x20, 0x14);
    }
}