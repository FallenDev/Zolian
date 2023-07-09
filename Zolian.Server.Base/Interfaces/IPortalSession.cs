using Darkages.Network.Client;

namespace Darkages.Interfaces;

public interface IPortalSession
{
    void TransitionToMap(WorldClient client, int destinationMap = 0) { }

    void ShowFieldMap(WorldClient client) { }
}