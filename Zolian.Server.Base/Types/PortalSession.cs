using Darkages.Database;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Templates;

using System.Collections.Concurrent;

namespace Darkages.Types;

public class PortalSession : IPortalSession
{
    public void TransitionToMap(GameClient client, int destinationMap = 0)
    {
        var readyTime = DateTime.Now;
        client.LastWarp = readyTime.AddMilliseconds(100);
        client.ResetLocation(client);

        if (destinationMap == 0)
        {
            client.Aisling.Abyss = true;
            ShowFieldMap(client);
            client.Send(new ServerFormat19(client, 42));
        }

        client.Aisling.Abyss = false;
    }

    public void ShowFieldMap(GameClient client)
    {
        if (client.MapOpen) return;

        //GenerateFieldMap();

        if (ServerSetup.Instance.GlobalWorldMapTemplateCache.TryGetValue(client.Aisling.World, out var worldMap))
        {
            if (worldMap.Portals.Any(ports => !ServerSetup.Instance.GlobalMapCache.ContainsKey(ports.Destination.AreaID)))
            {
                ServerSetup.Logger("No Valid Configured World Map.");
                return;
            }
        }

        client.Send(new ServerFormat2E(client.Aisling));
    }

    //public void GenerateFieldMap()
    //{
    //    ServerSetup.Instance.GlobalWorldMapTemplateCache = new ConcurrentDictionary<int, WorldMapTemplate>();
    //    DatabaseLoad.CacheFromDatabase(new WorldMapTemplate());
    //}
}