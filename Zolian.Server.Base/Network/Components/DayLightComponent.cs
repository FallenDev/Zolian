using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class DayLightComponent : WorldServerComponent
{
    private readonly WorldServerTimer _timer = new(TimeSpan.FromSeconds(15.0f));
    private static readonly SortedDictionary<int, (byte start, byte end)> Routine = new()
    {
        {0, (0, 0)},
        {1, (0, 1)},
        {2, (1, 2)},
        {3, (2, 3)},
        {4, (3, 4)},
        {5, (4, 5)},
        {6, (5, 4)},
        {7, (4, 3)},
        {8, (3, 2)},
        {9, (2, 1)},
        {10, (1, 0)},
        {11, (0, 0)}
    };

    public DayLightComponent(WorldServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(UpdateDayLight);
    }

    private static void UpdateDayLight()
    {
        if (ServerSetup.Instance.LightPhase >= 12)
            ServerSetup.Instance.LightPhase = 0;

        var (start, end) = Routine.First(x => ServerSetup.Instance.LightPhase == x.Key).Value;

        if (ServerSetup.Instance.LightLevel == start)
            ServerSetup.Instance.LightLevel = end;

        foreach (var player in Server.Aislings)
        {
            player?.Client.SendLightLevel((LightLevel)ServerSetup.Instance.LightLevel);
        }

        ServerSetup.Instance.LightPhase++;
    }
}