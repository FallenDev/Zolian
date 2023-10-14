using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class CommunityComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(SaveCommunity);
    }

    private static void SaveCommunity()
    {
        if (ServerSetup.Instance.Game == null || !Server.Aislings.Any()) return;
        ServerSetup.SaveCommunityAssets();
    }
}