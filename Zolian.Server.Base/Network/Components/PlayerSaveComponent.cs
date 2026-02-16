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

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static async Task UpdatePlayerSaveAsync()
    {
        try
        {
            await StorageManager.AislingBucket.ServerSave(Server.Aislings);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"PlayerSaveComponent failed to perform a server save", LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }
}