using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites;

namespace Darkages.Network.Components;

public class PlayerStatusBarAndThreatComponent(WorldServer server) : WorldServerComponent(server)
{
    private static readonly Stopwatch StatusControl = new();
    private const int ComponentSpeed = 100;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(1);
                continue;
            }

            _ = UpdatePlayerStatusBarAndThreat();
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

    private static async Task UpdatePlayerStatusBarAndThreat()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;
        const int maxConcurrency = 10;
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var playerUpdateTasks = new List<Task>();

        if (!StatusControl.IsRunning)
            StatusControl.Start();

        if (StatusControl.Elapsed.TotalMilliseconds < 1000) return;

        try
        {
            var players = Server.Aislings.Where(player => player?.Client != null).ToList();

            foreach (var player in players)
            {
                await semaphore.WaitAsync();
                var task = ProcessUpdates(player).ContinueWith(t =>
                    {
                        semaphore.Release();
                    }, TaskScheduler.Default);

                playerUpdateTasks.Add(task);
            }

            await Task.WhenAll(playerUpdateTasks);
            StatusControl.Restart();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static Task ProcessUpdates(Aisling player)
    {
        player.UpdateBuffs(player);
        player.UpdateDebuffs(player);
        player.ThreatGeneratedSubsided(player);
        return Task.CompletedTask;
    }
}