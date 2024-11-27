using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Darkages.GameScripts.Areas.Generic;

[Script("Hell")]
public class Hell : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public Hell(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1 + 2000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_playersOnMap.IsEmpty)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);
    }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (!_playersOnMap.IsEmpty)
            _animate = true;
    }

    public override void OnMapExit(WorldClient client)
    {
        _playersOnMap.TryRemove(client.Aisling.Serial, out _);

        if (_playersOnMap.IsEmpty)
            _animate = false;
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;

        for (var i = 0; i < 6; i++)
        {
            var randA = RandomNumberGenerator.GetInt32(41);
            var randB = RandomNumberGenerator.GetInt32(41);
            _playersOnMap.Values.FirstOrDefault()?.SendAnimationNearby(384, new Position(randA, randB));
        }
    }
}