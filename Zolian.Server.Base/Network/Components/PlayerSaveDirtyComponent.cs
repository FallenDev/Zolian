using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerSaveDirtyComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 5000;

    protected internal override async Task Update()
    {
        var sw = Stopwatch.StartNew();
        var target = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            var elapsed = sw.Elapsed.TotalMilliseconds;
            if (elapsed < target)
            {
                var remaining = (int)(target - elapsed);
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 1000));
                continue;
            }

            await UpdateDirtyPlayersAsync();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private async Task UpdateDirtyPlayersAsync()
    {
        try
        {
            var dirtyPlayers = Server.Aislings.Where(a => a.PlayerSaveDirty).ToArray();
            if (dirtyPlayers.Length == 0) return;

            foreach (var aisling in dirtyPlayers)
            {
                // Ensure cached player is still loggged in
                if (aisling?.Client?.Aisling == null) continue;

                // Player save, dirty flag is reset after successful save within AislingStorage.PlayerSaveRoutine()
                _ = await aisling.Client.Save();
            }
        }
        catch { }
    }
}