using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("Mourning Room")]
public class MilethDreamSword : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public MilethDreamSword(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(8000));
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

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (newLocation.X != 11 || newLocation.Y != 4) return;

        client.TransitionToMap(3036, new Position(1, 2));
        client.SendServerMessage(ServerMessageType.ActiveMessage, "You've awoken, like a dream... where am I?");
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;

        _playersOnMap.Values.FirstOrDefault()?.SendAnimationNearby(86, new Position(11, 4));
    }

    public override void OnGossip(WorldClient client, string message) { }
}