using Darkages.Network.Client;

namespace Darkages.Interfaces
{
    public interface IPortalSession
    {
        void TransitionToMap(GameClient client, int destinationMap = 0) { }

        void ShowFieldMap(GameClient client) { }
    }
}