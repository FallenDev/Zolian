using Chaos.Common.Definitions;
using Darkages.Infrastructure;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class MessageClearComponent : WorldServerComponent
{
    public MessageClearComponent(WorldServer server) : base(server)
    {
        Timer = new WorldServerTimer(TimeSpan.FromSeconds(ServerSetup.Instance.Config.MessageClearInterval));
    }

    private WorldServerTimer Timer { get; set; }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (Timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(Message);
    }

    private void Message()
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