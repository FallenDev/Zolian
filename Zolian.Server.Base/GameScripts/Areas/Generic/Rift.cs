using Darkages.Common;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Generic;

[Script("Rift")]
public class Rift : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public Rift(Area area) : base(area)
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
        client.SendServerMessage(ServerMessageType.ActiveMessage, "Strong one, temper your will!");
        client.SendSound((byte)Random.Shared.Next(119), true);
        if (!_playersOnMap.IsEmpty)
            _animate = true;
    }

    public override void OnMapExit(WorldClient client)
    {
        _playersOnMap.TryRemove(client.Aisling.Serial, out _);

        if (!_playersOnMap.IsEmpty) return; 
        _animate = false;
        var monsters = ObjectManager.GetObjects<Monster>(Area, p => p is { Alive: true });
        foreach (var monster in monsters)
            monster.Value.Remove();
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;

        for (var i = 0; i < 6; i++)
        {
            var randA = Random.Shared.Next(0, 40);
            var randB = Random.Shared.Next(80, 119);
            _playersOnMap.Values.FirstOrDefault()?.SendAnimationNearby(384, new Position(randA, randB));
        }
    }
}