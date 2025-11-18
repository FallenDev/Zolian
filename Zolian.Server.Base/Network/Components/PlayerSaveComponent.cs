using System.Diagnostics;

using Darkages.Database;
using Darkages.Network.Server;
using Microsoft.Extensions.Logging;

namespace Darkages.Network.Components;

public class PlayerSaveComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 45000;

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

            await UpdatePlayerSaveAsync();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            if (overshoot > 0 && overshoot < ComponentSpeed)
            {
                target = ComponentSpeed - (int)overshoot;
            }
            else
            {
                target = ComponentSpeed;
            }

            sw.Restart();
        }
    }

    private async Task UpdatePlayerSaveAsync()
    {
        var playersList = Server.Aislings.ToList();
        if (playersList.Count == 0) return;

        try
        {
            await StorageManager.AislingBucket.ServerSave(playersList);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"PlayerSaveComponent failed to save {playersList.Count} players: {e}", LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }
}