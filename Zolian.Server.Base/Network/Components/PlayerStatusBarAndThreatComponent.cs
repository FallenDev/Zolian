using Darkages.Network.Server;

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
                if (!player.Client.StatusControl.IsRunning)
                    player.Client.StatusControl.Start();

                if (player.Client.StatusControl.Elapsed.TotalMilliseconds < 1000) return;

                player.UpdateBuffs(TimeSpan.FromMilliseconds(1000));
                player.UpdateDebuffs(TimeSpan.FromMilliseconds(1000));
                player.ThreatGeneratedSubsided(player);

                player.Client.StatusControl.Restart();
            });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}