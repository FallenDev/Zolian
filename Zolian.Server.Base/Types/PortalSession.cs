using Darkages.Interfaces;
using Darkages.Network.Client;

namespace Darkages.Types;

public class PortalSession : IPortalSession
{
    public void TransitionToMap(WorldClient client, int destinationMap = 0)
    {
        var readyTime = DateTime.UtcNow;
        client.LastWarp = readyTime.AddMilliseconds(100);
        client.LeaveArea(destinationMap, true, true);
        client.ResetLocation(client);

        if (destinationMap == 0)
        {
            ShowFieldMap(client);
            client.SendSound(42, true);
        }
    }

    public void ShowFieldMap(WorldClient client)
    {
        if (client.MapOpen) return;

        //GenerateFieldMap();

        if (ServerSetup.Instance.GlobalWorldMapTemplateCache.TryGetValue(client.Aisling.World, out var worldMap))
        {
            if (worldMap.Portals.Any(ports => !ServerSetup.Instance.GlobalMapCache.ContainsKey(ports.Destination.AreaID)))
            {
                ServerSetup.EventsLogger("No Valid Configured World Map.");
                return;
            }
        }

        client.SendWorldMap();
    }

    //public void GenerateFieldMap()
    //{
    //    ServerSetup.Instance.GlobalWorldMapTemplateCache = new ConcurrentDictionary<int, WorldMapTemplate>();
    //    DatabaseLoad.CacheFromDatabase(new WorldMapTemplate());
    //}
}