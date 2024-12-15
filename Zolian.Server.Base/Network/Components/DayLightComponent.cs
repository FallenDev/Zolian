using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class DayLightComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 15000;
    private static readonly SortedDictionary<int, (byte start, byte end)> Routine = new()
    {
        {0, (0, 0)},
        {1, (0, 1)},
        {2, (1, 2)},
        {3, (2, 3)},
        {4, (3, 4)},
        {5, (4, 5)},
        {6, (5, 4)},
        {7, (4, 3)},
        {8, (3, 2)},
        {9, (2, 1)},
        {10, (1, 0)},
        {11, (0, 0)}
    };

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(500);
                continue;
            }

            UpdateDayLight();
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

    private static void UpdateDayLight()
    {
        if (ServerSetup.Instance.LightPhase >= 12)
            ServerSetup.Instance.LightPhase = 0;

        var (start, end) = Routine.First(x => ServerSetup.Instance.LightPhase == x.Key).Value;

        if (ServerSetup.Instance.LightLevel == start)
            ServerSetup.Instance.LightLevel = end;

        Parallel.ForEach(Server.Aislings, (player) =>
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;
            player.Client.SendLightLevel((LightLevel)ServerSetup.Instance.LightLevel);
        });

        ServerSetup.Instance.LightPhase++;
    }
}