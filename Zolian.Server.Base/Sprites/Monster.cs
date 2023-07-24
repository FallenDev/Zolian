using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;
using Darkages.Dialogs.Abstractions;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Infrastructure;
using Darkages.Scripting;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Sprites;

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
        TaggedAislings = new ConcurrentDictionary<long, bool>();
        AggroList = new List<long>();
        TileType = TileContent.Monster;
    }

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
    public long DamageReceived { get; set; }
    private bool Rewarded { get; set; }
    public ConcurrentDictionary<string, MonsterScript> Scripts { get; private set; }
    public readonly List<SkillScript> SkillScripts = new();
    public readonly List<SkillScript> AbilityScripts = new();
    public readonly List<SpellScript> SpellScripts = new();
    public List<long> AggroList { get; init; }
    public List<Item> MonsterBank { get; set; }
    public bool Skulled { get; set; }
    public bool Blind => HasDebuff("Blind");
    public ConcurrentDictionary<long, bool> TaggedAislings { get; set; }
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
        TaggedAislings ??= new ConcurrentDictionary<long, bool>();

        if (target is not Aisling aisling) return;

        var alreadyTagged = TaggedAislings.TryGetValue(aisling.Serial, out var taggedNearby);
        var playerNearby = AislingsEarShotNearby().Contains(aisling);

        switch (alreadyTagged)
        {
            case false:
                TaggedAislings.TryAdd(aisling.Serial, true);
                break;
            case true:
                if (!playerNearby)
                    TaggedAislings.TryRemove(aisling.Serial, out _);
                break;
        }

        if (aisling.GroupParty != null && aisling.GroupParty.PartyMembers.Count - 1 <= 0) return;
        if (aisling.GroupParty == null) return;

        foreach (var member in aisling.GroupParty.PartyMembers.Where(member => member != null))
        {
            var memberTagged = TaggedAislings.TryGetValue(member.Serial, out _);
            var playersNearby = AislingsEarShotNearby().Contains(member);

            switch (memberTagged)
            {
                case false:
                    TaggedAislings.TryAdd(member.Serial, true);
                    break;
                case true:
                    if (!playersNearby)
                        TaggedAislings.TryRemove(member.Serial, out _);
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

    public IEnumerable<Aisling> GetTaggedAislings() => TaggedAislings.Any() ? TaggedAislings.Select(b => GetObject<Aisling>(Map, n => n.Serial == b.Key)).Where(i => i != null).ToList() : new List<Aisling>();

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
        if (Map.IsWall((int)nodeX, (int)nodeY) || Map.IsAStarSprite(this, (int)nodeX, (int)nodeY))
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