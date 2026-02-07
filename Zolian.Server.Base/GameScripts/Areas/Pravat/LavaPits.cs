using System.Collections.Concurrent;

using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Pravat;

[Script("LavaPits")]
public class LavaPits : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    public LavaPits(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
        client.SendServerMessage(ServerMessageType.ActiveMessage, "The heat of the Lava Pits engulfs you...");
    }

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    }

    public override void OnNpcWalk(Movable npc)
    {
        if (_playersOnMap.IsEmpty) return;

        try
        {
            if (Area.TileContent[npc.Position.X, npc.Position.Y] == Enums.TileContent.Wall && npc is Monster monster)
                monster.SendAnimationNearby(223, npc.Position);
        }
        catch { }
    }
}