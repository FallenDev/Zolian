﻿using Chaos.Common.Definitions;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Fight Hall")]
public class FightHall : AreaScript
{
    private Aisling _aisling;

    public FightHall(Area area) : base(area)
    {
        Area = area;
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 195) return;
        if (!_aisling.Client.Aisling.IsDead()) return;
        _aisling.Client.GhostFormToAisling();
        _aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
    }

    public override void OnMapEnter(WorldClient client)
    {
        _aisling = client.Aisling;
        if (_aisling == null) return;
        if (_aisling.Map.ID != 195) return;
        if (!_aisling.Client.Aisling.IsDead()) return;
        _aisling.Client.GhostFormToAisling();
        _aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Be careful out there.");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}