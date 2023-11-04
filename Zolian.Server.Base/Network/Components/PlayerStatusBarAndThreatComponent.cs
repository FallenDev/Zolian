using Darkages.Network.Server;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Components;

public class PlayerStatusBarAndThreatComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdatePlayerStatusBarAndThreat);
    }

    private static void UpdatePlayerStatusBarAndThreat()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        try
        {
            Parallel.ForEach(Server.Aislings, (player) =>
            {
                if (player?.Client == null) return;
                if (!player.LoggedIn) return;

                if (!player.Client.StatusControl.IsRunning)
                {
                    player.Client.StatusControl.Start();
                }

                if (player.Client.StatusControl.Elapsed.TotalMilliseconds < player.BuffAndDebuffTimer.Delay.TotalMilliseconds) return;

                player.UpdateBuffs(player.Client.StatusControl.Elapsed);
                player.UpdateDebuffs(player.Client.StatusControl.Elapsed);
                player.ThreatGeneratedSubsided(player);
                player.Client.StatusControl.Restart();
            });
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }
}