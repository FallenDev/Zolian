using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites.Entity;

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
        Server.ForEachLoggedInAisling(async static player =>
        {
            try
            {
                if (!player.PlayerSaveDirty) return;
                _ = await player.Client.Save();
            }
            catch { }
        });
    }
}