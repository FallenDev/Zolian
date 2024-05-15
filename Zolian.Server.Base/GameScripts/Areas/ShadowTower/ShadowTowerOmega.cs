using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.IdentityModel.Tokens;

using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Object;

namespace Darkages.GameScripts.Areas.ShadowTower;

[Script("Shadow Tower Omega")]
public class ShadowTowerOmega : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public ShadowTowerOmega(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

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