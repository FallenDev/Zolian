using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class ClientCreationLimit(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(RemoveLimit);
    }

    private static void RemoveLimit()
    {
        if (!ServerSetup.Instance.Running) return;

        foreach (var (ip, creationCount) in ServerSetup.Instance.GlobalCreationCount)
        {
            if (creationCount > 0)
            {
                var countMod = creationCount;
                countMod--;
                ServerSetup.Instance.GlobalCreationCount.TryUpdate(ip, countMod, creationCount);
            }

            if (creationCount == 0)
                ServerSetup.Instance.GlobalCreationCount.TryRemove(ip, out _);
        }
    }
}