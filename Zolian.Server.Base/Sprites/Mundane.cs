using System.Collections.Concurrent;
using System.Numerics;
using System.Security.Cryptography;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Darkages.Common;
using Darkages.Dialogs.Abstractions;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Scripting;
using Darkages.Templates;
using Darkages.Types;

using ServiceStack;

namespace Darkages.Sprites;

public sealed class Mundane : Sprite, IDialogSourceEntity
{
    private readonly List<SkillScript> _skillScripts = new();
    private readonly List<SpellScript> _spellScripts = new();
    private int _waypointIndex;
    private Position CurrentWaypoint => Template.Waypoints[_waypointIndex];
    public ConcurrentDictionary<string, MundaneScript> Scripts { get; private set; }
    public MundaneTemplate Template { get; init; }
    public bool Bypass { get; set; }

    public Mundane()
    {
        TileType = TileContent.Mundane;
    }

    public static void Create(MundaneTemplate template)
    {
        if (template == null) return;

        var map = ServerSetup.Instance.GlobalMapCache[template.AreaID];
        var existing = template.GetObject<Mundane>(map, p => p?.Template != null && p.Template.Name == template.Name);

        if (existing != null) return;

        var npc = new Mundane
        {
            Template = template
        };

        if (npc.Template.TurnRate == 0)
            npc.Template.TurnRate = 5;

        if (npc.Template.CastRate == 0)
            npc.Template.CastRate = 2;

        if (npc.Template.WalkRate == 0)
            npc.Template.WalkRate = 2;

        npc.CurrentMapId = npc.Template.AreaID;
        npc.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        npc.Pos = new Vector2(template.X, template.Y);
        npc.Direction = npc.Template.Direction;
        npc.CurrentMapId = npc.Template.AreaID;

        if (npc.Template.ChatRate == 0) npc.Template.ChatRate = 10;

        if (npc.Template.TurnRate == 0) npc.Template.TurnRate = 8;

        npc.DefenseElement = Generator.RandomEnumValue<ElementManager.Element>();
        npc.OffenseElement = Generator.RandomEnumValue<ElementManager.Element>();

        npc.Scripts = ScriptManager.Load<MundaneScript>(template.ScriptKey, ServerSetup.Instance.Game, npc);
        if (npc.Scripts != null)
            ServerSetup.Instance.GlobalMundaneScriptCache.TryAdd(npc.Template.Name, npc.Scripts.Values.FirstOrDefault());

        npc.Template.AttackTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(450));
        npc.Template.EnableTurning = false;
        npc.Template.WalkTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.WalkRate));
        npc.Template.ChatTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.ChatRate));
        npc.Template.TurnTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.TurnRate));
        npc.Template.SpellTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.CastRate));

        npc.InitMundane();
        ServerSetup.Instance.GlobalMundaneCache.TryAdd(npc.Serial, npc);
        npc.AddObject(npc);
    }

    private void InitMundane()
    {
        if (Template.Spells != null)
            foreach (var spellScriptStr in Template.Spells)
                LoadSpellScript(spellScriptStr);

        if (Template.Skills != null)
            foreach (var skillScriptStr in Template.Skills)
                LoadSkillScript(skillScriptStr);

        LoadSkillScript("Assail", true);
    }

    private void LoadSkillScript(string skillScriptStr, bool primary = false)
    {
        Skill obj;
        var scripts = ScriptManager.Load<SkillScript>(skillScriptStr,
            obj = Skill.Create(1, ServerSetup.Instance.GlobalSkillTemplateCache[skillScriptStr]));

        foreach (var script in scripts.Values)
        {
            if (script == null) continue;
            script.Skill = obj;
            _skillScripts.Add(script);
        }
    }

    private void LoadSpellScript(string spellScriptStr, bool primary = false)
    {
        var scripts = ScriptManager.Load<SpellScript>(spellScriptStr,
            Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache[spellScriptStr]));

        foreach (var script in scripts.Values.Where(script => script != null))
        {
            script.IsScriptDefault = primary;
            _spellScripts.Add(script);
        }
    }

    private void Patrol()
    {
        if (CurrentWaypoint != null) WalkTo(CurrentWaypoint.X, CurrentWaypoint.Y);

        if (Position.DistanceFrom(CurrentWaypoint) > 2 && CurrentWaypoint != null) return;
        if (_waypointIndex + 1 < Template.Waypoints.Count)
            _waypointIndex++;
        else
            _waypointIndex = 0;
    }

    public void Update(TimeSpan update)
    {
        if (Template == null) return;

        var nearby = GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this));

        if (Template.ChatTimer != null && Template.EnableSpeech)
        {
            Template.ChatTimer.UpdateTime(update);

            var speak = Generator.RandNumGen100();

            if (Template.ChatTimer.Elapsed && Template.Speech.Count > 0)
            {
                if (speak >= 50)
                {
                    var idx = Random.Shared.Next(Template.Speech.Count);
                    var msg = Template.Speech[idx];

                    if (!msg.IsNullOrEmpty())
                        foreach (var aisling in nearby)
                        {
                            aisling.Client.SendPublicMessage(Serial, PublicMessageType.Normal, $"{Template.Name}: {msg}");
                        }
                }

                Template.ChatTimer.Reset();
            }
        }

        if (Template.EnableTurning)
            if (Template.TurnTimer != null)
            {
                Template.TurnTimer.Update(update);
                if (Template.TurnTimer.Elapsed)
                {
                    Direction = (byte)RandomNumberGenerator.GetInt32(5);
                    Turn();
                    Template.TurnTimer.Reset();
                }
            }

        if (!Template.EnableWalking) return;
        {
            var a = Template.WalkTimer.Update(update);

            if (!a) return;
            if (Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Template.Waypoints == null)
                {
                    Wander();
                }
                else
                {
                    if (Template.Waypoints?.Count > 0)
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

    public DisplayColor Color => DisplayColor.Default;
    public EntityType EntityType => EntityType.Creature;
    public uint Id => Serial;
    public string Name => Template.Name;
    public ushort Sprite => Template.Image;
    public void Activate(Aisling source) => Scripts.First().Value.OnClick(source.Client, Id);
}