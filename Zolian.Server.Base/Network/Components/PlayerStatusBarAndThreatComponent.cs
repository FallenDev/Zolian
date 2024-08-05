using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerStatusBarAndThreatComponent(WorldServer server) : WorldServerComponent(server)
{
    private static readonly object StatusControlLock = new();
    private static readonly Stopwatch StatusControl = new();

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdatePlayerStatusBarAndThreat);
    }

    private static void UpdatePlayerStatusBarAndThreat()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        try
        {
            lock (StatusControlLock)
            {
                if (!StatusControl.IsRunning)
                    StatusControl.Start();

                if (StatusControl.Elapsed.TotalMilliseconds < 1000) return;

                Parallel.ForEach(Server.Aislings, (player) =>
                {
                    if (player?.Client == null) return;
                    player.UpdateBuffs(player);
                    player.UpdateDebuffs(player);
                    player.ThreatGeneratedSubsided(player);
                });

                Console.Write($"------------------ Routine\n");
                StatusControl.Restart();
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}