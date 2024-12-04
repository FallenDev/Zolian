﻿using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Types;

using Microsoft.IdentityModel.Tokens;
using System.Numerics;
using Darkages.Object;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.ShadowTower;

[Script("Shadow Tower Omega")]
public class ShadowTowerOmega : AreaScript
{
    public ShadowTowerOmega(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("Rob34", out var boss);
        var mobsOnMap = client.Aisling.MonstersOnMap();
        if (!mobsOnMap.IsNullOrEmpty()) return;
        var bossCreate = Monster.Create(boss, client.Aisling.Map);
        if (bossCreate == null) return;
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, "Omega Draconic, now online!"));
        ObjectManager.AddObject(bossCreate);
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}