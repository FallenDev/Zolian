﻿using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Generic Area")]
public class GenericArea : AreaScript
{
    private Sprite _aisling;

    public GenericArea(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;
    public override void OnMapExit(WorldClient client) => _aisling = null;
    public override void OnMapClick(WorldClient client, int x, int y) => _aisling ??= client.Aisling;
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}