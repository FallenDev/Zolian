using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Evermore HallB")]
public class EvermoreHallsB : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private readonly Vector2 _poleTrap1;
    private readonly Vector2 _poleTrap2;
    private readonly Vector2 _poleTrap3;
    private readonly Vector2 _spikeTrap1;
    private readonly Vector2 _spikeTrap2;
    private readonly Vector2 _spikeTrap3;
    private readonly Stopwatch _vectorRoll = new();

    public EvermoreHallsB(Area area) : base(area)
    {
        Area = area;
        _poleTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
    }

    public override void Update(TimeSpan elapsedTime)
    {
        List<Vector2> rollingTraps = [];

        if (!_vectorRoll.IsRunning)
        {
            _vectorRoll.Start();
        }

        if (_vectorRoll.Elapsed.TotalSeconds < 3) return;
        var randRollingDmg = Generator.RandNumGen100();

        switch (randRollingDmg)
        {
            case >= 0 and <= 30:
                rollingTraps.Add(new Vector2(1, 19));
                rollingTraps.Add(new Vector2(1, 18));
                rollingTraps.Add(new Vector2(2, 19));
                rollingTraps.Add(new Vector2(2, 18));
                rollingTraps.Add(new Vector2(4, 19));
                rollingTraps.Add(new Vector2(4, 18));
                rollingTraps.Add(new Vector2(5, 19));
                rollingTraps.Add(new Vector2(5, 18));
                rollingTraps.Add(new Vector2(6, 19));
                rollingTraps.Add(new Vector2(6, 18));
                rollingTraps.Add(new Vector2(7, 19));
                rollingTraps.Add(new Vector2(7, 18));
                rollingTraps.Add(new Vector2(8, 19));
                rollingTraps.Add(new Vector2(8, 18));
                rollingTraps.Add(new Vector2(9, 19));
                rollingTraps.Add(new Vector2(9, 18));
                RollingSpikeTraps(rollingTraps);
                break;
            case <= 60:
                rollingTraps.Add(new Vector2(1, 14));
                rollingTraps.Add(new Vector2(1, 13));
                rollingTraps.Add(new Vector2(2, 14));
                rollingTraps.Add(new Vector2(2, 13));
                rollingTraps.Add(new Vector2(3, 14));
                rollingTraps.Add(new Vector2(3, 13));
                rollingTraps.Add(new Vector2(4, 14));
                rollingTraps.Add(new Vector2(4, 13));
                rollingTraps.Add(new Vector2(6, 14));
                rollingTraps.Add(new Vector2(6, 13));
                rollingTraps.Add(new Vector2(7, 14));
                rollingTraps.Add(new Vector2(7, 13));
                rollingTraps.Add(new Vector2(8, 14));
                rollingTraps.Add(new Vector2(8, 13));
                rollingTraps.Add(new Vector2(9, 14));
                rollingTraps.Add(new Vector2(9, 13));
                RollingSpikeTraps(rollingTraps);
                break;
            case <= 90:
                rollingTraps.Add(new Vector2(1, 9));
                rollingTraps.Add(new Vector2(1, 8));
                rollingTraps.Add(new Vector2(2, 9));
                rollingTraps.Add(new Vector2(2, 8));
                rollingTraps.Add(new Vector2(3, 9));
                rollingTraps.Add(new Vector2(3, 8));
                rollingTraps.Add(new Vector2(4, 9));
                rollingTraps.Add(new Vector2(4, 8));
                rollingTraps.Add(new Vector2(5, 9));
                rollingTraps.Add(new Vector2(5, 8));
                rollingTraps.Add(new Vector2(6, 9));
                rollingTraps.Add(new Vector2(6, 8));
                rollingTraps.Add(new Vector2(8, 9));
                rollingTraps.Add(new Vector2(8, 8));
                rollingTraps.Add(new Vector2(9, 9));
                rollingTraps.Add(new Vector2(9, 8));
                RollingSpikeTraps(rollingTraps);
                break;
            case <= 100:
                _vectorRoll.Restart();
                return;
        }

        rollingTraps.Clear();
        _vectorRoll.Restart();
    }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        var safe1 = new Vector2(4, 1);
        var safe2 = new Vector2(4, 0);
        var safe3 = new Vector2(4, 23);
        var safe4 = new Vector2(4, 24);

        if (client.Aisling.Pos != vectorMap) return;
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        if (vectorMap == safe1 || vectorMap == safe2 || vectorMap == safe3 || vectorMap == safe4) return;

        if (vectorMap == _poleTrap1 || vectorMap == _poleTrap2 || vectorMap == _poleTrap3)
        {
            OnPoleTrap(client);
        }

        if (vectorMap == _spikeTrap1 || vectorMap == _spikeTrap2 || vectorMap == _spikeTrap3)
        {
            OnSpikeTrap(client);
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private static void OnPoleTrap(WorldClient client)
    {
        client.SendAnimation(140, client.Aisling.Position);
        client.Aisling.ApplyTrapDamage(client.Aisling, 500000, 59);
    }

    private static void OnSpikeTrap(WorldClient client)
    {
        client.SendAnimation(112, client.Aisling.Position);
        client.Aisling.ApplyTrapDamage(client.Aisling, 750000, 68);
    }

    private void RollingSpikeTraps(List<Vector2> trapList)
    {
        foreach (var trapPosition in trapList)
        {
            if (_playersOnMap.IsEmpty) return;
            _playersOnMap.Values.FirstOrDefault()?.SendAnimationNearby(112, new Position(trapPosition));

            foreach (var player in _playersOnMap.Values)
            {
                if (player == null) continue;
                if (player.Pos != trapPosition) continue;
                player.ApplyTrapDamage(player, 750000, 68);
            }
        }
    }
}