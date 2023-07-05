using Darkages.Database;
using Darkages.Infrastructure;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerSaveComponent : GameServerComponent
{
    private readonly GameServerTimer _timer = new(TimeSpan.FromSeconds(1));

    public PlayerSaveComponent(WorldServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime))
        {
            ZolianUpdateDelegate.Update(UpdatePlayerSave);
        }
    }

    private static async void UpdatePlayerSave()
    {
        if (!ServerSetup.Instance.Running || Server.Aislings == null) return;
        foreach (var player in Server.Aislings)
        {
            if (!player.LoggedIn) continue;

            await StorageManager.AislingBucket.QuickSave(player);

            var readyTime = DateTime.UtcNow;
            if ((readyTime - player.Client.LastSave).TotalSeconds > ServerSetup.Instance.Config.SaveRate)
                await player.Client.Save();
        }
    }
}