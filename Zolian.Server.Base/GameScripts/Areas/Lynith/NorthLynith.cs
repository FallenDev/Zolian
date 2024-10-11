using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Enums;

namespace Darkages.GameScripts.Areas.Lynith;

[Script("North Lynith")]
public class NorthLynith : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public NorthLynith(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        switch (newLocation.X)
        {
            case 15 when newLocation.Y == 9:
                if (client.Aisling.QuestManager.ScubaGearCrafted && client.Aisling.EquipmentManager.Equipment[16]?.Item.Template.Name == "Scuba Gear"
                    || client.Aisling.Race.RaceFlagIsSet(Race.Merfolk))
                {
                    client.TransitionToMap(5110, new Position(2, 14));
                    return;
                }

                client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm afraid I'll drown if I slip under this rock!");
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}