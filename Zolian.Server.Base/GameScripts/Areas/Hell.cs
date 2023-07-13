using System.Numerics;
using System.Security.Cryptography;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Hell")]
public class Hell : AreaScript
{
    private Sprite _aisling;
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public Hell(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1 + 2000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 23352)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);
    }

    public override void OnMapEnter(WorldClient client)
    {
        _aisling = client.Aisling;
        _animate = true;
    }

    public override void OnMapExit(WorldClient client)
    {
        _aisling = null;
        _animate = false;
    }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (_aisling?.Map.ID != 23352) return;
        if (!a) return;

        for (var i = 0; i < 6; i++)
        {
            var randA = RandomNumberGenerator.GetInt32(41);
            var randB = RandomNumberGenerator.GetInt32(41);
            _aisling?.Show(Scope.Self, new ServerFormat29(384, new Vector2(randA, randB), 0x32));
        }
    }
}