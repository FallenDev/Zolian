using System.Diagnostics;

using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class UpdateClientsComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 50;

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
                    await Task.Delay(Math.Min(remaining, 5));

                continue;
            }

            UpdateClientRoutine();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private void UpdateClientRoutine()
    {
        var players = Server.Aislings.ToArray();
        if (players.Length == 0) return;

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player == null) continue;
            var client = player.Client;
            if (client == null) continue;

            try
            {
                if (!player.LoggedIn)
                {
                    try
                    {
                        client.CloseTransport();
                        ServerSetup.Instance.Game.WorldClientRegistry.TryRemove(client.Id, out _);
                    }
                    catch { }
                    continue;
                }

                client.Update();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);

                try
                {
                    client.CloseTransport();
                    ServerSetup.Instance.Game.WorldClientRegistry.TryRemove(client.Id, out _);
                }
                catch { }
                continue;
            }
        }
    }
}