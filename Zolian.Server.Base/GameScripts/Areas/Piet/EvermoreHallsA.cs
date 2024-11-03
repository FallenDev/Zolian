using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Common;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Evermore HallA")]
public class EvermoreHallsA : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private readonly Vector2 _poleTrap1;
    private readonly Vector2 _poleTrap2;
    private readonly Vector2 _poleTrap3;
    private readonly Vector2 _poleTrap4;
    private readonly Vector2 _poleTrap5;
    private readonly Vector2 _spikeTrap1;
    private readonly Vector2 _spikeTrap2;
    private readonly Vector2 _spikeTrap3;
    private readonly Vector2 _spikeTrap4;
    private readonly Vector2 _spikeTrap5;

    public EvermoreHallsA(Area area) : base(area)
    {
        Area = area;
        _poleTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap4 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap5 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap4 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap5 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
    }

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (vectorMap == _poleTrap1 || vectorMap == _poleTrap2 || vectorMap == _poleTrap3 ||
            vectorMap == _poleTrap4 || vectorMap == _poleTrap5)
        {
            OnPoleTrap(client);
        }

        if (vectorMap == _spikeTrap1 || vectorMap == _spikeTrap2 || vectorMap == _spikeTrap3 ||
            vectorMap == _spikeTrap4 || vectorMap == _spikeTrap5)
        {
            OnSpikeTrap(client);
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private static void OnPoleTrap(WorldClient client)
    {
        client.SendAnimation(140, client.Aisling.Position);
        client.Aisling.ApplyTrapDamage(client.Aisling, 50000, 59);
    }

    private static void OnSpikeTrap(WorldClient client)
    {
        client.SendAnimation(112, client.Aisling.Position);
        client.Aisling.ApplyTrapDamage(client.Aisling, 75000, 68);
    }
}