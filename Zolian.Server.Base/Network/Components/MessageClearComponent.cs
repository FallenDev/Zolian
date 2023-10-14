using Chaos.Common.Definitions;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class MessageClearComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(Message);
    }

    private static void Message()
    {
        foreach (var player in Server.Aislings)
        {
            if (player.Client == null) continue;
            var readyTime = DateTime.UtcNow;
            if ((readyTime - player.Client.LastMessageSent).TotalSeconds > 5)
                player.Client.SendServerMessage(ServerMessageType.OrangeBar1, "\u0000");
        }
    }
}