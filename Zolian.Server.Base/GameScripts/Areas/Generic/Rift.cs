using Darkages.Common;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using Darkages.Enums;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;

namespace Darkages.GameScripts.Areas.Generic;

[Script("Rift")]
public class Rift : AreaScript
{
    private int _monstersOnMap;
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];
    private WorldServerTimer AnimTimer { get; }
    private WorldServerTimer BossTimer { get; }
    private bool _animate;

    public Rift(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(2000));
        BossTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_playersOnMap.IsEmpty)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);

        var a = BossTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;
        _monstersOnMap = ObjectManager.GetObjects<Monster>(Area, p => p is { Alive: true }).Count;
        if (_monstersOnMap != 0) return;

        var riftBossKilled = _playersOnMap.Values.FirstOrDefault(p => p.MonsterKillCounters["Rift Boss"].TotalKills >= 1);
        if (riftBossKilled is not null)
        {
            // ToDo: Create rewards for main player who killed the boss

            foreach (var player in _playersOnMap.Values)
            {
                player.Client.TransitionToMap(188, new Position(12, 22));
            }

            return;
        }

        var topKiller = _playersOnMap.Values.OrderByDescending(p => p.MonsterKillCounters["Rift Mob"].TotalKills).FirstOrDefault();
        if (topKiller is null) return;
        if (topKiller.MonsterKillCounters["Rift Mob"].TotalKills >= 20 && !topKiller.Client.SummonRiftBoss)
        {
            topKiller.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(114, true));
            SummonRiftBoss(topKiller.Client);
        }
    }

    public override void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
        client.SendServerMessage(ServerMessageType.ActiveMessage, "Strong one, temper your will!");
        client.SendSound((byte)Random.Shared.Next(119), true);
        if (!_playersOnMap.IsEmpty)
            _animate = true;
        client.Aisling.MonsterKillCounters.Clear();
    }

    public override void OnMapExit(WorldClient client)
    {
        _playersOnMap.TryRemove(client.Aisling.Serial, out _);
        client.SummonRiftBoss = false;
        client.Aisling.MonsterKillCounters.Clear();

        if (!_playersOnMap.IsEmpty) return;

        _animate = false;
        var monsters = ObjectManager.GetObjects<Monster>(Area, p => p is { Alive: true });
        foreach (var monster in monsters)
            monster.Value.Remove();
    }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) =>
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;
        if (_playersOnMap.IsEmpty) return;

        for (var i = 0; i < 6; i++)
        {
            var randA = Random.Shared.Next(0, 40);
            var randB = Random.Shared.Next(80, 119);
            _playersOnMap.Values.FirstOrDefault()?.SendAnimationNearby(384, new Position(randA, randB));
        }
    }

    private void SummonRiftBoss(WorldClient client)
    {
        var sprites = new List<int>
        {
            12, 376, 377, 379, 380, 397, 402, 403, 404, 411, 417
        };

        var boss = new MonsterTemplate
        {
            ScriptName = "Rift",
            BaseName = "Rift Boss",
            Name = $"{Random.Shared.NextInt64()}RiftBoss",
            AreaID = Area.ID,
            Image = (ushort)sprites.RandomIEnum(),
            ElementType = ElementQualifer.Defined,
            OffenseElement = ElementManager.Element.Terror,
            DefenseElement = ElementManager.Element.Terror,
            PathQualifer = PathQualifer.Wander,
            SpawnType = SpawnQualifer.Defined,
            DefinedX = (ushort)client.Aisling.X,
            DefinedY = (ushort)client.Aisling.Y,
            SpawnSize = 1,
            MoodType = MoodQualifer.Aggressive,
            MonsterType = MonsterType.Boss,
            MonsterArmorType = Enum.GetValues<MonsterArmorType>().RandomIEnum(),
            MonsterRace = MonsterRace.Demon,
            IgnoreCollision = true,
            Waypoints = [],
            MovementSpeed = 800,
            EngagedWalkingSpeed = Random.Shared.Next(800, 1400),
            AttackSpeed = Random.Shared.Next(500, 1000),
            CastSpeed = Random.Shared.Next(3000, 6000),
            LootType = LootQualifer.RandomGold,
            Level = (ushort)(client.Aisling.ExpLevel + client.Aisling.AbpLevel + Random.Shared.Next(1, 15)),
            SkillScripts = [],
            AbilityScripts = [],
            SpellScripts = []
        };

        var monster = Monster.Create(boss, Area);
        if (monster == null) return;
        ObjectManager.AddObject(monster);

        foreach (var (serial, players) in _playersOnMap)
        {
            players.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bYou have proven yourself worthy.");
            players.Client.SummonRiftBoss = true;
        }
    }
}