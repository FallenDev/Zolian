﻿using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("Sanctum Cleanse")]
public class SanctumCleansingPool : AreaScript
{
    public SanctumCleansingPool(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        if (vectorMap != new Vector2(9, 7) &&
            vectorMap != new Vector2(10, 7) &&
            vectorMap != new Vector2(8, 8) &&
            vectorMap != new Vector2(9, 8) &&
            vectorMap != new Vector2(10, 8) &&
            vectorMap != new Vector2(11, 8) &&
            vectorMap != new Vector2(9, 9) &&
            vectorMap != new Vector2(10, 9) &&
            vectorMap != new Vector2(11, 9)) return;
        foreach (var debuff in client.Aisling.Debuffs.Values)
        {
            debuff?.OnEnded(client.Aisling, debuff);
        }

        if (!client.Aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Normal))
        {
            client.Aisling.Afflictions |= Afflictions.Normal;
            client.SendServerMessage(ServerMessageType.ActiveMessage, "Your afflictions have been removed!");
        }

        client.Aisling.SendAnimationNearby(195, new Position(vectorMap));
    }
}