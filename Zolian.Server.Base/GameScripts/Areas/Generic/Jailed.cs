﻿using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Generic;

[Script("Jailed")]
public class Jailed : AreaScript
{
    public Jailed(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnMapClick(WorldClient client, int x, int y)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Nothing but silence...");
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"The smell of blood fills the room");
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}