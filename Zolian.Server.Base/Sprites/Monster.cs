using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Dialogs.Abstractions;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.ScriptingBase;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Sprites;

public record TargetRecord
{
    /// <summary>
    /// Serial, Damage, Player, Nearby
    /// </summary>
    public ConcurrentDictionary<long, (long dmg, Aisling player, bool nearby)> TaggedAislings { get; set; }
}

public sealed class Monster : Sprite, IDialogSourceEntity
{
    public Task<IList<Vector2>> Path;

    public Monster()
    {
        BashEnabled = false;
        AbilityEnabled = false;
        CastEnabled = false;
        WalkEnabled = false;
        ObjectUpdateEnabled = false;
        WaypointIndex = 0;
        TileType = TileContent.Monster;
        TargetRecord = new TargetRecord
        {
            TaggedAislings = new ConcurrentDictionary<long, (long dmg, Aisling player, bool nearby)>()
        };
    }

    public TargetRecord TargetRecord { get; set; }
    public bool Aggressive { get; set; }
    public bool ThrownBack { get; set; }
    public bool AStar { get; set; }
    public bool BashEnabled { get; set; }
    public bool AbilityEnabled { get; set; }
    public bool CastEnabled { get; set; }
    public bool ObjectUpdateEnabled { get; set; }
    public uint Experience { get; set; }
    public string Size { get; set; }
    public ushort Image { get; set; }
    public bool WalkEnabled { get; set; }
    public WorldServerTimer BashTimer { get; init; }
    public WorldServerTimer AbilityTimer { get; init; }
    public WorldServerTimer CastTimer { get; init; }
    public MonsterTemplate Template { get; set; }
    public WorldServerTimer WalkTimer { get; init; }
    public WorldServerTimer ObjectUpdateTimer { get; init; }
    public bool IsAlive => CurrentHp > 0;
    private bool Rewarded { get; set; }
    public ConcurrentDictionary<string, MonsterScript> Scripts { get; private set; }
    public readonly List<SkillScript> SkillScripts = new();
    public readonly List<SkillScript> AbilityScripts = new();
    public readonly List<SpellScript> SpellScripts = new();

    public List<Item> MonsterBank { get; set; }
    public bool Skulled { get; set; }
    public bool Blind => HasDebuff("Blind");
    private int WaypointIndex;
    public Aisling Summoner => GetObject<Aisling>(Map, b => b.Serial == SummonerId);
    private Position CurrentWaypoint => Template?.Waypoints?[WaypointIndex];
    public long SummonerId { get; set; }

    public static Monster Create(MonsterTemplate template, Area map)
    {
        var monsterCreateScript = ScriptManager.Load<MonsterCreateScript>(ServerSetup.Instance.Config.MonsterCreationScript,
                template,
                map)
            .FirstOrDefault();

        return monsterCreateScript.Value?.Create();
    }

    public static void InitScripting(MonsterTemplate template, Area map, Monster obj)
    {
        obj.Scripts = ScriptManager.Load<MonsterScript>(template.ScriptName, obj, map);
        if (obj.Scripts != null)
            ServerSetup.Instance.GlobalMonsterScriptCache.TryAdd(obj.Template.Name, obj.Scripts.Values.FirstOrDefault());
    }

    public void TryAddTryRemoveTagging(Sprite target)
    {
        TargetRecord.TaggedAislings ??= new ConcurrentDictionary<long, (long dmg, Aisling player, bool nearby)>();

        if (target is not Aisling aisling) return;

        var alreadyTagged = TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out _);
        var playerNearby = AislingsEarShotNearby().Contains(aisling);

        switch (alreadyTagged)
        {
            case false:
                TargetRecord.TaggedAislings.TryAdd(aisling.Serial, (0, aisling, playerNearby));
                break;
            case true:
                if (!playerNearby)
                    TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                break;
        }

        if (aisling.GroupParty != null && aisling.GroupParty.PartyMembers.Count - 1 <= 0) return;
        if (aisling.GroupParty == null) return;

        foreach (var member in aisling.GroupParty.PartyMembers.Where(member => member != null))
        {
            var memberTagged = TargetRecord.TaggedAislings.TryGetValue(member.Serial, out _);
            var playersNearby = AislingsEarShotNearby().Contains(member);

            switch (memberTagged)
            {
                case false:
                    TargetRecord.TaggedAislings.TryAdd(member.Serial, (0, member, playersNearby));
                    break;
                case true:
                    if (!playersNearby)
                        TargetRecord.TaggedAislings.TryRemove(member.Serial, out _);
                    break;
            }
        }
    }

    public void GenerateRewards(Aisling player)
    {
        if (Rewarded) return;
        if (player.Equals(null)) return;
        if (player.Client.Aisling == null) return;

        var script = ScriptManager.Load<RewardScript>(ServerSetup.Instance.Config.MonsterRewardScript, this, player).FirstOrDefault();
        script.Value?.GenerateRewards(this, player);

        Rewarded = true;
        player.UpdateStats();
    }

    public void Patrol()
    {
        if (CurrentWaypoint != null) WalkTo(CurrentWaypoint.X, CurrentWaypoint.Y);

        if (Position.DistanceFrom(CurrentWaypoint) > 1 && CurrentWaypoint != null) return;
        if (WaypointIndex + 1 < Template.Waypoints.Count)
            WaypointIndex++;
        else
            WaypointIndex = 0;
    }

    public void AStarPath(Monster monster, IList<Vector2> pathList)
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

        var nodeX = pathList[0].X;
        var nodeY = pathList[0].Y;

        // Check if path became blocked, if so recalculate path
        if (Map.IsWall((int)nodeX, (int)nodeY) || Map.IsSpriteInLocationOnWalk(this, (int)nodeX, (int)nodeY))
        {
            Wander();
            return;
        }

        WalkTo((int)nodeX, (int)nodeY);
    }

    public DisplayColor Color => DisplayColor.Default;
    public EntityType EntityType => EntityType.Creature;
    public uint Id => Serial;
    public string Name => Template.BaseName;
    public ushort Sprite => Template.Image;
    public void Activate(Aisling source) => Scripts.First().Value.OnClick(source.Client);
}