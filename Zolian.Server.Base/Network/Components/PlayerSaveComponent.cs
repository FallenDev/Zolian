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

        var playersList = Server.Aislings.ToList();
        StorageManager.AislingBucket.ServerSave(playersList);

        foreach (var player in playersList.Where(player => player?.Client != null).Where(player => player.LoggedIn))
        {
            _ = StorageManager.AislingBucket.AuxiliarySave(player);
        }
    }
}