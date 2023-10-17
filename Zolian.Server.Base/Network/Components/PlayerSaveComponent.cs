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

        foreach (var player in Server.Aislings)
        {
            if (player?.Client == null) continue;
            if (!player.LoggedIn) continue;
            _ = StorageManager.AislingBucket.QuickSave(player);

            if ((readyTime - player.Client.LastSave).TotalSeconds > 4)
                _ = player.Client.Save();
        }
    }
}