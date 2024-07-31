using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.Sprites;

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
                if (!player.Client.StatusControl.IsRunning)
                    player.Client.StatusControl.Start();

                if (player.Client.StatusControl.Elapsed.TotalMilliseconds < 1000) return;
                
                PlayerSecondaryOffenseReset(player);
                player.UpdateBuffs();
                player.UpdateDebuffs();
                player.ThreatGeneratedSubsided(player);
                player.Client.StatusControl.Restart();
            });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void PlayerSecondaryOffenseReset(Aisling player)
    {
        if (player.EquipmentManager.Shield == null && player.SecondaryOffensiveElement != ElementManager.Element.None)
            player.SecondaryOffensiveElement = ElementManager.Element.None;
    }
}