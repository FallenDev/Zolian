using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class DayLightComponent : WorldServerComponent
{
    private readonly WorldServerTimer _timer = new(TimeSpan.FromSeconds(20.0f));

    public DayLightComponent(WorldServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(UpdateDayLight);
    }

    private static void UpdateDayLight()
    {
        ServerSetup.Instance.LightLevel++;

        if (ServerSetup.Instance.LightLevel >= 6)
            ServerSetup.Instance.LightLevel = 0;

        foreach (var player in Server.Aislings)
        {
            player?.Client.SendLightLevel((LightLevel)ServerSetup.Instance.LightLevel);
        }
    }
}