﻿using Darkages.Common;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class AreaScript(Area area) : IScriptBase
{
    protected Area Area = area;
    public WorldServerTimer Timer { get; set; }

    public abstract void Update(TimeSpan elapsedTime);

    public virtual void OnMapEnter(WorldClient client) { }
    public virtual void OnMapExit(WorldClient client) { }
    public virtual void OnMapClick(WorldClient client, int x, int y) { }
    public virtual void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public virtual void OnItemDropped(WorldClient client, Item item, Position location) { }
    public virtual void OnGossip(WorldClient client, string message) { }
}