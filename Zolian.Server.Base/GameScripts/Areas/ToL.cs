﻿using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("ToL")] // Temple of Light Area Map
public class ToL : AreaScript
{
    private Aisling _aisling;
    private WorldServerTimer AnimTimer { get; set; }
    private WorldServerTimer AnimTimer2 { get; set; }
    private bool _animate;

    public ToL(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1 + 5000));
        AnimTimer2 = new WorldServerTimer(TimeSpan.FromMilliseconds(2500));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 14758)
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

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        if (x == 15 && y == 52 || x == 14 && y == 51 || x == 14 && y == 50)
        {
            client.SendBoard("Hunting", null);
        }
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        var b = AnimTimer2.Update(elapsedTime);

        if (_aisling?.Map.ID != 14758) return;
        if (a)
        {
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(192, new Position(15, 55)));
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(192, new Position(20, 55)));
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(192, new Position(21, 40)));
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(192, new Position(14, 40)));
        }

        if (b)
        {
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(96, new Position(17, 59)));
            _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(96, new Position(18, 59)));
        }
    }
}