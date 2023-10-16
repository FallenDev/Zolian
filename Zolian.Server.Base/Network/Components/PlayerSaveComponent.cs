using Darkages.Database;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerSaveComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdatePlayerSave);
    }

    private static void UpdatePlayerSave()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;
        var readyTime = DateTime.UtcNow;

        Parallel.ForEach(Server.Aislings, (player) =>
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;
            _ = StorageManager.AislingBucket.QuickSave(player);

            if ((readyTime - player.Client.LastSave).TotalSeconds > ServerSetup.Instance.Config.SaveRate)
                _ = player.Client.Save();
        });
    }
}