using System.Numerics;
using System.Security.Cryptography;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Templates;
using Darkages.Types;

using ServiceStack;

namespace Darkages.Sprites.Entity;

public sealed class Mundane : Movable
{
    public string Name => Template.Name;
    public ushort Sprite => Template.Image;

    private int _waypointIndex;
    private Position CurrentWaypoint
    {
        get => Template.Waypoints[_waypointIndex];
        set => Template.Waypoints[_waypointIndex] = value;
    }

    public MundaneScript AIScript { get; set; }
    public MundaneTemplate Template { get; init; }
    public bool Bypass { get; set; }
    public bool GuardModeActivated { get; set; }

    public Mundane()
    {
        TileType = TileContent.Mundane;
    }

    public static void Create(MundaneTemplate template)
    {
        if (template == null) return;

        var map = ServerSetup.Instance.GlobalMapCache[template.AreaID];
        var existing = ObjectManager.GetObject<Mundane>(map, p => p.Template.Name == template.Name);

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
        npc.InitMundane(npc.Template, ServerSetup.Instance.Game, npc);

        if (npc.Template.ChatRate == 0) npc.Template.ChatRate = 5;
        if (npc.Template.TurnRate == 0) npc.Template.TurnRate = 8;

        npc.Template.EnableTurning = false;
        npc.Template.WalkTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.WalkRate));
        npc.Template.ChatTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.ChatRate));
        npc.Template.TurnTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.TurnRate));

        ServerSetup.Instance.GlobalMundaneCache.TryAdd(npc.Serial, npc);

        // Temporary npc by map cache
        AddMundaneByArea(ServerSetup.Instance.TempMundaneByMapCache, npc);

        ObjectManager.AddObject(npc);
    }

    private void InitMundane(MundaneTemplate template, WorldServer server, Mundane obj)
    {
        if (ScriptManager.TryCreate<MundaneScript>(template.ScriptKey, out var aiScript, server, obj))
            obj.AIScript = aiScript;
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

    public void Update(TimeSpan update)
    {
        if (Template == null) return;

        if (Template.ChatTimer != null && Template.EnableSpeech)
        {
            Template.ChatTimer.UpdateTime(update);
            var speak = Generator.RandomPercentPrecise();

            if (Template.ChatTimer.Elapsed && Template.Speech.Count > 0)
            {
                if (speak >= .70)
                {
                    var idx = Random.Shared.Next(Template.Speech.Count);
                    var msg = Template.Speech[idx];

                    if (!msg.IsNullOrEmpty())
                    {
                        var nearby = ObjectManager.GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this));

                        foreach (var (_, aisling) in nearby)
                        {
                            aisling.Client.SendPublicMessage(Serial, PublicMessageType.Normal, $"{Template.Name}: {msg}");
                        }
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

    private static void AddMundaneByArea(Dictionary<int, List<Mundane>> byArea, Mundane template)
    {
        var areaId = template.CurrentMapId;

        if (!byArea.TryGetValue(areaId, out var list))
        {
            list = [];
            byArea[areaId] = list;
        }

        list.Add(template);
    }
}