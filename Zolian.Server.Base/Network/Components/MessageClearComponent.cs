using System.Diagnostics;

using Darkages.Database;
using Darkages.Network.Server;
using Darkages.Templates;

namespace Darkages.Network.Components;

public class MessageClearComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 60_000;

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
                    await Task.Delay(Math.Min(remaining, 500));
                continue;
            }

            ClearPlayerMessages();
            RefreshBoardCache();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void ClearPlayerMessages()
    {
        var now = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(5);

        Server.ForEachLoggedInAisling(state: (now, timeout),
            action: static (player, s) =>
            {
                try
                {
                    var sinceLast = s.now - player.Client.LastMessageSent;
                    if (sinceLast > s.timeout)
                    {
                        // Clear the orange bar message
                        player.Client.SendServerMessage(ServerMessageType.OrangeBar1, "\u0000");
                    }
                }
                catch { }
            });
    }

    private static void RefreshBoardCache()
    {
        try
        {
            ServerSetup.Instance.GlobalBoardPostCache.Clear();
            BoardPostStorage.CacheFromDatabase(AislingStorage.PersonalMailString);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}