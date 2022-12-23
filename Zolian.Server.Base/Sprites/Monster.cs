using System.Collections.Concurrent;
using System.Numerics;

using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Infrastructure;
using Darkages.Scripting;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Sprites
{
    public sealed class Monster : Sprite
    {
        public Monster()
        {
            BashEnabled = false;
            AbilityEnabled = false;
            CastEnabled = false;
            WalkEnabled = false;
            ObjectUpdateEnabled = false;
            WaypointIndex = 0;
            TaggedAislings = new HashSet<int>();
            AggroList = new List<int>();
            EntityType = TileContent.Monster;
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
        public GameServerTimer BashTimer { get; init; }
        public GameServerTimer AbilityTimer { get; init; }
        public GameServerTimer CastTimer { get; init; }
        public MonsterTemplate Template { get; set; }
        public GameServerTimer WalkTimer { get; init; }
        public GameServerTimer ObjectUpdateTimer { get; init; }
        public bool IsAlive => CurrentHp > 0;
        public long DamageReceived { get; set; }
        private bool Rewarded { get; set; }
        public ConcurrentDictionary<string, MonsterScript> Scripts { get; private set; }
        public readonly List<SkillScript> SkillScripts = new();
        public readonly List<SkillScript> AbilityScripts = new();
        public readonly List<SpellScript> SpellScripts = new();
        public List<int> AggroList { get; init; }
        public List<Item> MonsterBank { get; set; }
        public bool Skulled { get; set; }
        public bool Blind => HasDebuff("Blind");
        public HashSet<int> TaggedAislings { get; set; }
        private int WaypointIndex;
        public Aisling Summoner => GetObject<Aisling>(Map, b => b.Serial == SummonerId);
        private Position CurrentWaypoint => Template?.Waypoints?[WaypointIndex];
        public int SummonerId { get; set; }

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

        public void AppendTags(Sprite target)
        {
            TaggedAislings ??= new HashSet<int>();

            if (target is not Aisling aisling)
                return;

            if (!TaggedAislings.Contains(aisling.Serial))
                TaggedAislings.Add(aisling.Serial);

            if (aisling.GroupParty != null && aisling.GroupParty.PartyMembers.Count - 1 <= 0)
                return;

            if (aisling.GroupParty == null) return;

            foreach (var member in aisling.GroupParty.PartyMembers.Where(member =>
                !TaggedAislings.Contains(member.Serial)))
                TaggedAislings.Add(member.Serial);
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

        public IEnumerable<Aisling> GetTaggedAislings()
        {
            if (TaggedAislings.Any())
                return TaggedAislings.Select(b => GetObject<Aisling>(Map, n => n.Serial == b)).Where(i => i != null)
                    .ToList();

            return new List<Aisling>();
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

            WalkTo((int)nodeX, (int)nodeY);
        }
    }
}