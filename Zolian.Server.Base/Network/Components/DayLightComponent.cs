using Darkages.Infrastructure;
using Darkages.Network.Formats.Models.ServerFormats;

namespace Darkages.Network.Components;

public class DayLightComponent : GameServerComponent
{
    private readonly GameServerTimer _timer = new(TimeSpan.FromSeconds(20.0f));
    private byte _shade;

    public DayLightComponent(Server.GameServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (!_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(UpdateDayLight);
    }

    private void UpdateDayLight()
    {
        var format20 = new ServerFormat20 { Shade = _shade };

        lock (Server.Clients)
        {
            foreach (var client in Server.Clients.Values)
            {
                client?.Send(format20);
            }
        }

        _shade += 1;
        _shade %= 18;
    }
}