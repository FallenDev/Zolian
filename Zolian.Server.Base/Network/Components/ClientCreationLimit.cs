using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class ClientCreationLimit(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 3600000;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(60000);
                continue;
            }

            RemoveLimit();
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

    private static void RemoveLimit()
    {
        if (!ServerSetup.Instance.Running) return;

        foreach (var (ip, creationCount) in ServerSetup.Instance.GlobalCreationCount)
        {
            if (creationCount > 0)
            {
                var countMod = creationCount;
                countMod--;
                ServerSetup.Instance.GlobalCreationCount.TryUpdate(ip, countMod, creationCount);
            }

            if (creationCount == 0)
                ServerSetup.Instance.GlobalCreationCount.TryRemove(ip, out _);
        }
    }
}