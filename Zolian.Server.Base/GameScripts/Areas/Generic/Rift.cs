using System.Collections.Concurrent;
using System.Security.Cryptography;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

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

        // No players on map, do nothing.
        if (_playersOnMap.IsEmpty) return;

        // Check for alive monsters on the map.
        _monstersOnMap = ObjectService.CountWithPredicate<Monster>(Area, p => p is { Alive: true });

        // If there are still monsters alive, do nothing further.
        if (_monstersOnMap != 0) return;

        // Check if any player has killed the Rift Boss, allocate rewards if so.
        var playerWhoKilledBoss = _playersOnMap.Values.FirstOrDefault(p => p.MonsterKillCounters.ContainsKey("Rift Boss") && p.MonsterKillCounters["Rift Boss"]?.TotalKills >= 1);
        if (playerWhoKilledBoss is not null)
        {
            var guardianKillReward = _playersOnMap.RandomIEnum().Value;
            guardianKillReward ??= _playersOnMap.RandomIEnum().Value;
            //ChestGenerator(guardianKillReward.Client);

            foreach (var player in _playersOnMap.Values)
            {
                player.MonsterKillCounters.Clear();
                player.Client.SummonRiftBoss = false;
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bThe rift calms... for now. You feel a pull to leave this place.}");
                player.Client.TransitionToMap(188, new Position(12, 22));
            }

            playerWhoKilledBoss.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qCongratulations on killing the boss, here is your additional award.}");
            playerWhoKilledBoss.GiveGold((uint)Random.Shared.Next(15_000_000, 25_000_000));
            var legend = new Legend.LegendItem
            {
                Key = $"Rift{RandomNumberGenerator.GetInt32(int.MaxValue)}Boss{RandomNumberGenerator.GetInt32(int.MaxValue)}",
                IsPublic = true,
                Time = DateTime.UtcNow,
                Color = LegendColor.LightOrangeDarkOrangeG12,
                Icon = (byte)LegendIcon.Community,
                Text = "Decimated a Rift Guardian"
            };

            playerWhoKilledBoss.LegendBook.AddLegend(legend, playerWhoKilledBoss.Client);
            return;
        }

        // Determine the top killer of Rift Mobs to summon the Rift Boss.
        var topKiller = _playersOnMap.Values.Where(p => p.MonsterKillCounters.ContainsKey("Rift Mob")).OrderByDescending(p => p.MonsterKillCounters["Rift Mob"]?.TotalKills).FirstOrDefault();
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

        // If there are players on the map, enable animations.
        if (!_playersOnMap.IsEmpty)
            _animate = true;

        // Reset player's rift boss summon status and kill counters.
        client.SummonRiftBoss = false;
        client.Aisling.MonsterKillCounters.Clear();
    }

    public override void OnMapExit(WorldClient client)
    {
        _playersOnMap.TryRemove(client.Aisling.Serial, out _);

        // Player leaves map, reset their rift boss summon status and kill counters.
        if (client != null)
        {
            client.SummonRiftBoss = false;
            client.Aisling.MonsterKillCounters.Clear();
        }

        // If there are still players on the map, do nothing further.
        if (!_playersOnMap.IsEmpty) return;

        _animate = false;

        // If map is empty, remove all monsters.
        var monsters = SpriteQueryExtensions.MonstersOnMapSnapshot(Area);
        foreach (var monster in monsters)
            monster.Remove();
    }

    // When a player walks, ensure they are tracked in the players on map.
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) =>
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);
        if (!a) return;

        // No players on map, do nothing.
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
            ScriptName = "RiftMob",
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
            EngagedWalkingSpeed = 800,
            AttackSpeed = Random.Shared.Next(500, 750),
            CastSpeed = Random.Shared.Next(2000, 5000),
            LootType = LootQualifer.LootR,
            Level = (ushort)(client.Aisling.ExpLevel + client.Aisling.AbpLevel + Random.Shared.Next(7, 15)),
            SkillScripts = [],
            AbilityScripts = [],
            SpellScripts = []
        };

        var monster = Monster.Create(boss, Area);
        if (monster == null) return;
        ObjectManager.AddObject(monster);

        foreach (var (serial, players) in _playersOnMap)
        {
            players.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou have proven yourself worthy. {{=e{client.Aisling.Username} {{=bsummoned the Rift Guardian!");
            players.Client.SummonRiftBoss = true;
        }
    }

    private bool ChestGenerator(WorldClient client)
    {
        if (client == null) return false;
        var level = client.Aisling.Level + client.Aisling.AbpLevel;
        return level switch
        {
            >= 0 and <= 599 => client.GiveItem("Rift Chest lv. 500"),
            >= 600 and <= 699 => client.GiveItem("Rift Chest lv. 600"),
            >= 700 and <= 799 => client.GiveItem("Rift Chest lv. 700"),
            >= 800 and <= 899 => client.GiveItem("Rift Chest lv. 800"),
            >= 900 => client.GiveItem("Rift Chest lv. 900"),
            _ => false
        };
    }
}