using System.Diagnostics;

using Darkages.Database;
using Darkages.Network.Server;
using Darkages.Templates;

namespace Darkages.Network.Components;

public class MessageClearComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 60000;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(500);
                continue;
            }

            Message();
            UpdateBoards();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static void Message()
    {
        var readyTime = DateTime.UtcNow;

        Parallel.ForEach(Server.Aislings, (player) =>
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;
            if ((readyTime - player.Client.LastMessageSent).TotalSeconds > 5)
                player.Client.SendServerMessage(ServerMessageType.OrangeBar1, "\u0000");
        });
    }

    private static void UpdateBoards()
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