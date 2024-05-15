using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Object;

namespace Darkages.GameScripts.Areas.ShadowTower;

[Script("Shadow Games")]
public class ShadowGames : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public ShadowGames(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("Rob33", out var boss1);
        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("Rob32", out var boss2);

        switch (newLocation.X)
        {
            case 19 when newLocation.Y == 1:
                // blue
                if (client.Aisling.HasItem("Arc Source"))
                {
                    var item = client.Aisling.HasItemReturnItem("Arc Source");
                    client.Aisling.Inventory.RemoveFromInventory(client, item);
                    var bossCreate1 = Monster.Create(boss1, client.Aisling.Map);
                    if (bossCreate1 == null) return;
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, "Arc unit, now online!"));
                    ObjectManager.AddObject(bossCreate1);
                }
                break;
            case 7 when newLocation.Y == 1:
                // red
                if (client.Aisling.HasItem("Neo Source"))
                {
                    var item = client.Aisling.HasItemReturnItem("Neo Source");
                    client.Aisling.Inventory.RemoveFromInventory(client, item);
                    var bossCreate2 = Monster.Create(boss2, client.Aisling.Map);
                    if (bossCreate2 == null) return;
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, "Neo unit, now online!"));
                    ObjectManager.AddObject(bossCreate2);
                }
                break;
            case 10 when newLocation.Y == 11:
                // main boss
                if (client.Aisling.HasItem("Neo Processing Core") && client.Aisling.HasItem("Arc Processing Core"))
                {
                    var item1 = client.Aisling.HasItemReturnItem("Neo Processing Core");
                    client.Aisling.Inventory.RemoveFromInventory(client, item1);
                    var item2 = client.Aisling.HasItemReturnItem("Arc Processing Core");
                    client.Aisling.Inventory.RemoveFromInventory(client, item2);

                    if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                    {
                        foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 1510))
                        {
                            player.Client.TransitionToMap(1513, new Position(23, 23));
                        }

                        return;
                    }

                    client.TransitionToMap(1513, new Position(23, 23));
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}