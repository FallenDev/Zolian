using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Dialogs.Abstractions;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Interfaces;
using Darkages.ScriptingBase;
using Darkages.Templates;
using Darkages.Types;
using ServiceStack;

using System.Collections.Concurrent;
using System.Numerics;

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
    public uint Ability { get; set; }
    public string Size { get; set; }
    public ushort Image { get; set; }
    public bool WalkEnabled { get; set; }
    public WorldServerTimer BashTimer { get; init; }
    public WorldServerTimer AbilityTimer { get; init; }
    public WorldServerTimer CastTimer { get; init; }
    public MonsterTemplate Template { get; init; }
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
    public bool Camouflage => AbilityScripts.Any(script => script.Skill.Template.Name == "Camouflage");
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

    public static void InitScripting(MonsterTemplate template, Area map, Monster obj) => obj.Scripts = ScriptManager.Load<MonsterScript>(template.ScriptName, obj, map);

    public void TryAddPlayerAndHisGroup(Sprite target)
    {
        if (target is not Aisling aisling)
        {
            Target = target;
            return;
        }

        TargetRecord.TaggedAislings.TryAdd(aisling.Serial, (0, aisling, true));

        if (aisling.GroupParty != null && aisling.GroupParty.PartyMembers.IsEmpty()) return;
        if (aisling.GroupParty == null) return;

        foreach (var member in aisling.GroupParty.PartyMembers.Where(member => member != null))
        {
            var memberTagged = TargetRecord.TaggedAislings.TryGetValue(member.Serial, out _);
            var playersNearby = AislingsEarShotNearby().Contains(member);

            if (!memberTagged)
                TargetRecord.TaggedAislings.TryAdd(member.Serial, (0, member, playersNearby));
        }
    }

    public bool TryAddTagging(Sprite target)
    {
        if (target is not Aisling aisling) return true;
        var checkGroup = TargetRecord.TaggedAislings.FirstOrDefault().Value;

        if (checkGroup.player.GroupId != aisling.GroupId) return false;
        TargetRecord.TaggedAislings.TryAdd(aisling.Serial, (0, aisling, true));
        return true;
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

    public void GenerateInanimateRewards(Aisling player)
    {
        if (Rewarded) return;
        if (player.Equals(null)) return;
        if (player.Client.Aisling == null) return;

        var script = ScriptManager.Load<RewardScript>(ServerSetup.Instance.Config.MonsterRewardScript, this, player).FirstOrDefault();
        script.Value?.GenerateInanimateRewards(this, player);

        Rewarded = true;
        player.UpdateStats();
    }
    
    public void LoadAndCastSpellScriptOnDeath(string spellTemplate)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(spellTemplate, out var template)) return;
            var script = ScriptManager.Load<SpellScript>(spellTemplate,
                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache[spellTemplate]));

            if (script == null)
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {spellTemplate}\n");
                return;
            }

            script.FirstOrDefault().Value?.OnUse(this, this);
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
            foreach (var player in aisling.GroupParty.PartyMembers.Where(player => player.Map.ID == aisling.Map.ID))
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

        if (monster.Template.BaseName is "Undead Guard" or "Undead Wizard" && !player.LegendBook.HasKey("LLau1"))
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

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
        {
            if (npc.Value.Scripts is null) continue;
            if (npc.Value.Scripts.TryGetValue("Quest Helper", out var scriptObj))
            {
                scriptObj.OnResponse(aisling.Client, responseId, $"{npc.Value.Serial}");
            }
        }
    }

    public static void CreateFromTemplate(MonsterTemplate template, Area map)
    {
        var newObj = Create(template, map);

        if (newObj == null) return;
        ServerSetup.Instance.GlobalMonsterCache[newObj.Serial] = newObj;
        AddObject(newObj);
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