using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites.Abstractions;
using Darkages.Templates;
using Darkages.Types;

using ServiceStack;

using static ServiceStack.Diagnostics.Events;

namespace Darkages.Sprites.Entity;

public record TargetRecord
{
    /// <summary>
    /// Serial, Damage, Player, Nearby
    /// </summary>
    public ConcurrentDictionary<long, Aisling> TaggedAislings { get; init; }
}

public sealed class Monster : Damageable
{
    private List<Vector2> _path;
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;
    public string Name => Template.BaseName;

    public Monster()
    {
        BashEnabled = false;
        AbilityEnabled = false;
        CastEnabled = false;
        WalkEnabled = false;
        ObjectUpdateEnabled = false;
        _waypointIndex = 0;
        TileType = TileContent.Monster;
        TargetRecord = new TargetRecord
        {
            TaggedAislings = []
        };
        Summoned = false;
    }

    public TargetRecord TargetRecord { get; set; }
    public readonly Lock TaggedAislingsLock = new();
    public bool Aggressive { get; set; }
    public bool ThrownBack { get; set; }
    public bool AStar { get; private set; }
    public bool BashEnabled { get; set; }
    public bool AbilityEnabled { get; set; }
    public bool CastEnabled { get; set; }
    public bool ObjectUpdateEnabled { get; set; }
    public long Experience { get; set; }
    public uint Ability { get; set; }
    public string Size { get; set; }
    public ushort Image { get; set; }
    public bool WalkEnabled { get; set; }
    public bool NextToTargetFirstAttack { get; set; }
    public WorldServerTimer BashTimer { get; init; }
    public WorldServerTimer AbilityTimer { get; init; }
    public WorldServerTimer CastTimer { get; init; }
    public MonsterTemplate Template { get; init; }
    public WorldServerTimer WalkTimer { get; init; }
    public WorldServerTimer ObjectUpdateTimer { get; init; } = new(TimeSpan.FromMilliseconds(200));

    public bool IsAlive => CurrentHp > 0;
    private bool Rewarded { get; set; }
    public MonsterScript AIScript { get; set; }
    public readonly List<SkillScript> SkillScripts = [];
    public readonly List<SkillScript> AbilityScripts = [];
    public readonly List<SpellScript> SpellScripts = [];

    public List<Item> MonsterBank { get; set; }
    public bool Skulled { get; set; }
    public bool Camouflage => AbilityScripts.Any(script => script.Skill.Template.Name == "Camouflage");
    private int _waypointIndex;
    public Aisling Summoner => ObjectManager.GetObject<Aisling>(Map, b => b.Serial == SummonerId);
    private Position CurrentWaypoint => Template?.Waypoints?[_waypointIndex];
    private static long SummonerId { get; set; }
    public ushort SummonerAdjLevel { get; set; }
    public readonly Stopwatch TimeTillDead = new();
    public bool SwarmOnApproach { get; set; }

    public static Monster Create(MonsterTemplate template, Area map)
    {
        ScriptManager.TryCreate<MonsterCreateScript>(ServerSetup.Instance.Config.MonsterCreationScript, out var monsterCreateScript, template, map);
        return monsterCreateScript?.Create();
    }

    public static Monster Summon(MonsterTemplate template, Aisling summoner)
    {
        SummonerId = summoner.Serial;
        ScriptManager.TryCreate<MonsterCreateScript>(ServerSetup.Instance.Config.MonsterCreationScript, out var monsterCreateScript, template, summoner.Map);
        return monsterCreateScript?.Create();
    }

    public static void InitScripting(MonsterTemplate template, Area map, Monster obj)
    {
        if (ScriptManager.TryCreate<MonsterScript>(template.ScriptName, out var aiScript, obj, map))
            obj.AIScript = aiScript;
    }

    public void TryAddPlayerAndHisGroup(Sprite target)
    {
        if (target is not Aisling aisling) return;
        if (Summoned) return;

        Target = aisling;
        lock (TaggedAislingsLock)
        {
            TargetRecord.TaggedAislings.TryAdd(aisling.Serial, aisling);
        }

        if (aisling.GroupId == 0 || aisling.GroupParty == null) return;

        var partyList = aisling.GroupParty.PartyMembers.Values
            .Where(member => member != null && aisling.CurrentMapId == member.CurrentMapId).ToList();

        foreach (var member in partyList)
        {
            lock (TaggedAislingsLock)
            {
                var memberTagged = TargetRecord.TaggedAislings.TryGetValue(member.Serial, out _);
                var playersNearby = AislingsEarShotNearby().Contains(member);

                if (!memberTagged)
                    TargetRecord.TaggedAislings.TryAdd(member.Serial, member);
            }
        }
    }

    public bool TryAddTagging(Sprite target)
    {
        if (target is not Aisling aisling) return true;
        if (Summoned) return true;

        // Check if the Aisling is already tagged and belongs to the same group.
        if (TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var existingTag) && existingTag.GroupId == aisling.GroupId)
        {
            return true; // The player is already tagged, no need to add again.
        }

        // Otherwise, safely add or update the tag.
        lock (TaggedAislingsLock) // Lock while modifying the dictionary
        {
            // TryAdd will only add the entry if it does not already exist
            if (!TargetRecord.TaggedAislings.ContainsKey(aisling.Serial))
            {
                TargetRecord.TaggedAislings.TryAdd(aisling.Serial, aisling);
            }
            else
            {
                // If the entry exists, update it (this should be atomic because we're locking)
                TargetRecord.TaggedAislings[aisling.Serial] = aisling;
            }
        }

        return true;
    }

    public void GenerateRewards(Aisling player)
    {
        if (Rewarded) return;
        if (player.Equals(null)) return;
        if (player.Client.Aisling == null) return;

        ScriptManager.TryCreate<RewardScript>(ServerSetup.Instance.Config.MonsterRewardScript, out var script, this, player);
        script?.GenerateRewards(this, player);

        Rewarded = true;
        player.UpdateStats();
    }

    public void GenerateRiftRewards(Aisling player)
    {
        if (Rewarded) return;
        if (player.Equals(null)) return;
        if (player.Client.Aisling == null) return;

        ScriptManager.TryCreate<RewardScript>("Rift Rewards", out var script, this, player);
        script?.GenerateRewards(this, player);

        Rewarded = true;
        player.UpdateStats();
    }

    public void GenerateInanimateRewards(Aisling player)
    {
        if (Rewarded) return;
        if (player.Equals(null)) return;
        if (player.Client.Aisling == null) return;

        ScriptManager.TryCreate<RewardScript>(ServerSetup.Instance.Config.MonsterRewardScript, out var script, this, player);
        script?.GenerateInanimateRewards(this, player);

        Rewarded = true;
        player.UpdateStats();
    }

    public void LoadAndCastSpellScriptOnDeath(string spellTemplate)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(spellTemplate, out var template)) return;

            if (!ScriptManager.TryCreate<SpellScript>(spellTemplate, out var script, Spell.Create(1, template)) || script == null)
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {spellTemplate}\n");
                return;
            }

            script.OnUse(this, this);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    public static void UpdateKillCounters(Monster monster)
    {
        if (monster.Target is not Aisling aisling) return;
        var readyTime = DateTime.UtcNow;
        var killRecord = new KillRecord
        {
            TotalKills = 1,
            TimeKilled = readyTime
        };

        if (aisling.GroupParty is not null)
        {
            foreach (var player in aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == aisling.Map.ID))
            {
                if (!player.MonsterKillCounters.TryGetValue(monster.Template.BaseName, out var value))
                {
                    player.MonsterKillCounters.TryAdd(monster.Template.BaseName, killRecord);
                }
                else
                {
                    value.TotalKills++;
                    value.TimeKilled = readyTime;
                    player.MonsterKillCounters.TryUpdate(monster.Template.BaseName, value, value);
                }

                var hasKills = ServerSetup.Instance.GlobalKillRecordCache.TryGetValue(player.Serial, out var killRecords);
                if (hasKills)
                {
                    ServerSetup.Instance.GlobalKillRecordCache.TryUpdate(player.Serial, player.MonsterKillCounters, killRecords);
                }
                else
                {
                    ServerSetup.Instance.GlobalKillRecordCache.TryAdd(player.Serial, player.MonsterKillCounters);
                }

                QuestTracking(monster, player);
            }
        }
        else
        {
            if (!aisling.MonsterKillCounters.TryGetValue(monster.Template.BaseName, out var value))
            {
                aisling.MonsterKillCounters.TryAdd(monster.Template.BaseName, killRecord);
            }
            else
            {
                value.TotalKills++;
                value.TimeKilled = readyTime;
                aisling.MonsterKillCounters.TryUpdate(monster.Template.BaseName, value, value);
            }

            var hasKills = ServerSetup.Instance.GlobalKillRecordCache.TryGetValue(aisling.Serial, out var killRecords);
            if (hasKills)
            {
                ServerSetup.Instance.GlobalKillRecordCache.TryUpdate(aisling.Serial, aisling.MonsterKillCounters, killRecords);
            }
            else
            {
                ServerSetup.Instance.GlobalKillRecordCache.TryAdd(aisling.Serial, aisling.MonsterKillCounters);
            }

            QuestTracking(monster, aisling);
        }
    }

    private static void QuestTracking(Monster monster, Aisling player)
    {
        var killRecordFound = player.MonsterKillCounters.TryGetValue(monster.Template.BaseName, out var value);
        if (!killRecordFound) return;

        if (monster.Template.BaseName == player.QuestManager.KeelaKill)
        {
            var returnPlayer = player.QuestManager.KeelaCount <= value?.TotalKills;
            TrackingNpcAndText(player, 0x01, returnPlayer, $"{{=aKeela Quest: {{=q{value?.TotalKills} {{=akilled");
        }

        if (monster.Template.BaseName == player.QuestManager.NealKill)
        {
            var returnPlayer = player.QuestManager.NealCount <= value?.TotalKills;
            TrackingNpcAndText(player, 0x03, returnPlayer, $"{{=aNeal Quest: {{=q{value?.TotalKills} {{=akilled");
        }

        if (monster.Template.BaseName == "Mouse" && player.QuestManager.PeteKill != 0 && !player.QuestManager.PeteComplete)
        {
            var returnPlayer = player.QuestManager.PeteKill <= value?.TotalKills;
            TrackingNpcAndText(player, 0x05, returnPlayer, $"{{=aMead Quest: {{=q{value?.TotalKills} {{=akilled");
        }

        if (monster.Template.BaseName is "Undead Guard" or "Undead Wizard" && player.QuestManager.Lau == 1 && !player.LegendBook.HasKey("LLau1"))
        {
            var returnPlayer = player.HasKilled("Undead Guard", 5) && player.HasKilled("Undead Wizard", 5);
            player.MonsterKillCounters.TryGetValue("Undead Guard", out var monA);
            player.MonsterKillCounters.TryGetValue("Undead Wizard", out var monB);

            TrackingNpcAndText(player, 0x07, returnPlayer, $"{{=aQuest: Guard {{=q{monA?.TotalKills} {{=aWizard {{=q{monB?.TotalKills}");
        }
    }

    private static void TrackingNpcAndText(IAisling aisling, byte responseId, bool returnPlayer, string text = "")
    {
        aisling.Client.SendServerMessage(ServerMessageType.PersistentMessage, text);
        if (!returnPlayer) return;
        if (!ServerSetup.Instance.MundaneByMapCache.TryGetValue(14759, out var questHelper) || questHelper.Length == 0) return;
        if (!questHelper.TryGetValue<Mundane>(t => t.Name == "Nadia", out var mundane) || mundane == null) return;
        mundane.AIScript?.OnResponse(aisling.Client, responseId, $"{mundane.Serial}");
    }

    private void Patrol()
    {
        if (CurrentWaypoint != null) WalkTo(CurrentWaypoint.X, CurrentWaypoint.Y);

        if (Position.DistanceFrom(CurrentWaypoint) > 1 && CurrentWaypoint != null) return;
        if (_waypointIndex + 1 < Template.Waypoints.Count)
            _waypointIndex++;
        else
            _waypointIndex = 0;
    }

    private void AStarPath(Monster monster, List<Vector2> pathList)
    {
        if (monster == null) return;
        if (pathList == null)
        {
            Wander();
            return;
        }

        if (pathList.Count == 0)
        {
            Wander();
            return;
        }

        if (pathList[0].X < 0 || pathList[0].Y < 0 || pathList[0].X >= Map.Width || pathList[0].Y >= Map.Height)
        {
            Wander();
            return;
        }

        var nodeX = pathList[0].X;
        var nodeY = pathList[0].Y;
        WalkTo((int)nodeX, (int)nodeY);
    }

    public void UpdateTarget(bool ascending = false, bool shadowSight = false)
    {
        if (!ObjectUpdateEnabled) return;
        var nearbyPlayers = AislingsEarShotNearby();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            lock (TaggedAislingsLock)
            {
                TargetRecord.TaggedAislings.Clear();
            }
        }

        AddNewlyGroupedPlayers();

        if (Aggressive)
        {
            var tagged = TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                var groupAttacking = ascending ? tagged.OrderBy(c => c.ThreatMeter) : tagged.OrderByDescending(c => c.ThreatMeter);

                foreach (var player in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn) continue;
                    if (!shadowSight && player.IsInvisible) continue;
                    if (player.Map != Map) continue;
                    Target = player;
                    break;
                }
            }
            else
            {
                if (Target != null) return;
                var topDps = ascending ? nearbyPlayers.OrderBy(c => c.ThreatMeter) : nearbyPlayers.OrderByDescending(c => c.ThreatMeter);

                foreach (var target in topDps)
                {
                    if (target.Skulled || !target.LoggedIn) continue;
                    if (!shadowSight && target.IsInvisible) continue;
                    if (target.Map != Map) continue;
                    Target = target;
                    break;
                }
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void AddNewlyGroupedPlayers()
    {
        if (TargetRecord.TaggedAislings.IsEmpty) return;
        var group = TargetRecord.TaggedAislings.FirstOrDefault()!.Value?.GroupParty?.PartyMembers;
        if (group == null) return;
        foreach (var (_, player) in group)
        {
            if (TargetRecord.TaggedAislings.Values.Contains(player)) continue;
            lock (TaggedAislingsLock)
            {
                TargetRecord.TaggedAislings.TryAdd(player.Serial, player);
            }
        }
    }

    public void ClearTarget()
    {
        CastEnabled = false;
        BashEnabled = false;
        WalkEnabled = true;

        if (Target is Aisling)
        {
            lock (TaggedAislingsLock)
            {
                TargetRecord.TaggedAislings.TryRemove(Target.Serial, out _);
            }
        }

        Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            _path?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public void SummonedCheckTarget()
    {
        if (Target is not Monster monster) return;
        if (!monster.Skulled) return;
        if (!monster.IsInvisible) return;
        Target = null;
    }

    public void SummonedClearTarget()
    {
        CastEnabled = false;
        BashEnabled = false;
        WalkEnabled = true;
        Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            _path?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public void PreWalkChecks()
    {
        if (CantMove) return;
        if (ThrownBack) return;

        while (Target != null)
        {
            if (Target is not Aisling aisling)
            {
                Wander();
                return;
            }

            if (aisling.IsInvisible || aisling.Dead || aisling.Skulled || !aisling.LoggedIn || Map.ID != aisling.Map.ID)
            {
                ClearTarget();
                Wander();
                return;
            }

            if (NextTo((int)Target.Pos.X, (int)Target.Pos.Y))
            {
                NextToTarget();
            }
            else
            {
                BeginPathFind();
            }

            return;
        }

        BashEnabled = false;
        CastEnabled = false;

        PatrolIfSet();
    }

    public void NextToTarget()
    {
        if (Target == null) return;
        NextToTargetFirstAttack = true;

        if (Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
        {
            BashEnabled = true;
            AbilityEnabled = true;
            CastEnabled = true;
        }
        else
        {
            BashEnabled = false;
            AbilityEnabled = true;
            CastEnabled = true;
            Direction = (byte)direction;
            Turn();
        }
    }

    public void BeginPathFind()
    {
        BashEnabled = false;
        CastEnabled = true;

        try
        {
            if (Target != null && Aggressive)
            {
                AStar = true;
                _location = new Vector2(Pos.X, Pos.Y);
                _targetPos = new Vector2(Target.Pos.X, Target.Pos.Y);
                _path = Map.FindPath(this, _location, _targetPos);

                if (ThrownBack) return;

                if (_targetPos == Vector2.Zero)
                {
                    ClearTarget();
                    Wander();
                    return;
                }

                if (_path.Count > 0)
                {
                    if (!_path.IsEmpty())
                        _path.RemoveAt(0);
                    AStarPath(this, _path);
                }

                if (_path.Count != 0) return;
                AStar = false;

                if (Target != null && WalkTo((int)Target.Pos.X, (int)Target.Pos.Y)) return;
            }
        }
        catch
        {
            // ignored
        }

        Wander();
    }

    public void PatrolIfSet()
    {
        if (Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
        {
            if (Template.Waypoints == null)
            {
                Wander();
            }
            else
            {
                if (Template.Waypoints.Count > 0)
                    Patrol();
                else
                    Wander();
            }
        }
        else
        {
            Wander();
        }
    }
}