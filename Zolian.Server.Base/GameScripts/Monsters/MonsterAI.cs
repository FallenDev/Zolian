using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;
using ServiceStack;

using System.Numerics;
using System.Security.Cryptography;

namespace Darkages.GameScripts.Monsters;

[Script("Common")]
public class BaseMonsterIntelligence : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public BaseMonsterIntelligence(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Weak Common")]
public class WeakCommon : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public WeakCommon(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on lowest threat
                var groupAttacking = tagged.OrderBy(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Lowest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on lowest threat
                    var topDps = targets.OrderBy(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Lowest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Inanimate")]
public class Inanimate : MonsterScript
{
    public Inanimate(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime) { }

    public override void OnClick(WorldClient client) => client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aHP: {Monster.CurrentHp}/{Monster.MaximumHp}");

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateInanimateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client) { }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true, false));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true, false), player);
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }
}

[Script("Shape Shifter")]
public class ShapeShifter : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public ShapeShifter(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var pct = Generator.RandomNumPercentGen();
            if (pct >= .92)
            {
                // Shape-shift to another sprite image
                if (Monster.Image != (ushort)Monster.Template.ImageVarience)
                {
                    Monster.Image = (ushort)Monster.Template.ImageVarience;

                    var objects = GetObjects(client.Aisling.Map, s => s.WithinRangeOf(client.Aisling), Get.AllButAislings).ToList();
                    objects.Reverse();

                    foreach (var aisling in Monster.AislingsNearby())
                    {
                        if (objects.Count == 0) continue;
                        aisling.Client.SendVisibleEntities(objects);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }

        base.OnDamaged(client, dmg, source);
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Image == Monster.Template.Image)
            {
                if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                {
                    if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                    {
                        Monster.BashEnabled = true;
                        Monster.AbilityEnabled = true;
                        Monster.CastEnabled = true;
                    }
                    else
                    {
                        Monster.BashEnabled = false;
                        Monster.AbilityEnabled = true;
                        Monster.CastEnabled = true;
                        Monster.Direction = (byte)direction;
                        Monster.Turn();
                    }

                    if (Monster.Target is not Aisling player) return;
                    Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                    Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.CastEnabled = true;

                    // Wander, AStar, and Standard Walk Methods
                    if (Monster.Target == null | !Monster.Aggressive)
                    {
                        Monster.Wander();
                    }
                    else
                    {
                        Monster.AStar = true;
                        _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                        Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                        Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                        if (Monster.ThrownBack) return;

                        if (Monster.TargetPos == Vector2.Zero)
                        {
                            Monster.ClearTarget();
                            Monster.Wander();
                            return;
                        }

                        if (Monster.Path.Result.Count > 0)
                        {
                            Monster.AStarPath(Monster, Monster.Path.Result);
                            if (!Monster.Path.Result.IsEmpty())
                                Monster.Path.Result.RemoveAt(0);
                        }

                        if (Monster.Path.Result.Count != 0) return;
                        Monster.AStar = false;

                        if (Monster.Target == null)
                        {
                            Monster.Wander();
                            return;
                        }

                        if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                        {
                            if (Monster.Target is not Aisling player) return;
                            Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                            Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                            return;
                        }

                        Monster.Wander();
                    }
                }
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.AbilityEnabled = true;
                Monster.CastEnabled = true;
                Monster.Wander();
                var pct = Generator.RandomNumPercentGen();
                if (pct >= .60)
                    CastSpell();
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Loot Goblin")]
public class LootGoblin : MonsterScript
{
    public LootGoblin(Monster monster, Area map) : base(monster, map)
    {
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        try
        {
            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}!!!!");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}!!!!");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void OnLeave(WorldClient client) { }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source) { }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        var walk = Monster.WalkTimer.Update(elapsedTime);
        if (!Monster.WalkEnabled) return;
        if (walk) Walk();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        Monster.Wander();
    }
}

[Script("Self Destruct")]
public class SelfDestruct : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public SelfDestruct(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Suicide();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Suicide()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 15) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.SkillScripts.Count);

        if (Monster.SkillScripts[abilityIdx] is null) return;
        var skill = Monster.SkillScripts[abilityIdx];
        skill.OnUse(Monster);
        if (Monster.Target == null) return;
        var suicide = Monster.CurrentHp / .5;
        Monster.Target.ApplyDamage(Monster, (long)suicide, null, true);
        OnDeath();
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Alert Summon")]
public class AlertSummon : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public AlertSummon(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast)
                    {
                        SummonMonsterNearby();
                    }
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void SummonMonsterNearby()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;

        var monstersNearby = Monster.MonstersOnMap();

        foreach (var monster in monstersNearby)
        {
            if (monster.WithinRangeOf(Monster)) continue;

            var readyTime = DateTime.UtcNow;
            monster.Pos = new Vector2(Monster.Pos.X, Monster.Pos.Y);

            foreach (var player in Monster.AislingsNearby())
            {
                player.Client.SendCreatureWalk(monster.Serial, new Point(Monster.X, Monster.Y), (Direction)Monster.Direction);
            }

            monster.LastMovementChanged = readyTime;
            monster.LastPosition = new Position(Monster.X, Monster.Y);
            break;
        }
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Turret")]
public class Turret : MonsterScript
{
    public Turret(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (!Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral)) return;
                Monster.Aggressive = false;
                Monster.ClearTarget();

                return;
            }

            if (assail) Bash();

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
            }

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var gatling = Monster.SkillScripts.FirstOrDefault(i => i.Skill.CanUse() && i.Skill.Template.Name == "Gatling");
        if (gatling?.Skill == null) return;

        gatling.Skill.InUse = true;
        gatling.OnUse(Monster);
        {
            var readyTime = DateTime.UtcNow;
            readyTime = readyTime.AddSeconds(gatling.Skill.Template.Cooldown);
            readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
            gatling.Skill.NextAvailableUse = readyTime;
        }
        gatling.Skill.InUse = false;
    }

    private void CastSpell()
    {
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Pirate")]
public class GeneralPirate : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _pirateSayings = "Arrr!|Arr! Let's sing a sea shanty.|Out'a me way!|Aye, yer sister is real nice|Gimmie yar gold!|Bet yar can't take me Bucko|Look at me funny and I'll a slit yar throat!|Scallywag!|Shiver my timbers|A watery grave for anyone who make us angry!|Arrr! Out'a me way and gimme yar gold!";
    private readonly string _pirateChase = "Ya landlubber can't run from us!|Harhar!! Running away eh?|Time fer a plundering!";
    private string[] Arggh => _pirateSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] Arrrgh => _pirateChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private int RunCount => Arrrgh.Length;
    private bool _deathCry;

    public GeneralPirate(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arrrgh[RandomNumberGenerator.GetInt32(RunCount + 1) % Arrrgh.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("PirateOfficer")]
public class PirateOfficer : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _pirateSayings = "Yo Ho|All Hands!|Hoist the colors high!|Ye, ho! All together!|Never shall we die!|Bet yar can't take me Bucko";
    private readonly string _pirateChase = "It will be the brig for ye!|All hands! We have a stowaway!|Haha!!";
    private string[] Arggh => _pirateSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] Arrrgh => _pirateChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private int RunCount => Arrrgh.Length;
    private bool _deathCry;

    public PirateOfficer(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var brigChance = Generator.RandomNumPercentGen();
        if (brigChance >= .95)
        {
            if (Monster.Target is not Aisling aisling) return;
            aisling.Client.TransitionToMap(6629, new Position(30, 15));
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bGot ye! To the brig!");
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.IsInvisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arrrgh[RandomNumberGenerator.GetInt32(RunCount + 1) % Arrrgh.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("ShadowSight")]
public class ShadowSight : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public ShadowSight(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Weak ShadowSight")]
public class WeakShadowSight : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    public WeakShadowSight(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsConfused || Monster.IsFrozen || Monster.IsStopped || Monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    Monster.ClearTarget();
                }

                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on lowest threat
                var groupAttacking = tagged.OrderBy(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Lowest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on lowest threat
                    var topDps = targets.OrderBy(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Lowest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack) return;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.ThrownBack) return;

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Aosda Remnant")]
public class AosdaRemnant : MonsterScript
{
    private Vector2 _location = Vector2.Zero;

    private readonly string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|Many years, have I walked this path";
    private readonly string _aosdaChase = "Come back, we can stay here together..  Forever|Don't leave me, anything but that";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public AosdaRemnant(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooooo"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Sweet release..        ^_^"));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ascradith Nem Tsu!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("AncientDragon")]
public class AncientDragon : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _aosdaSayings = "Young one, do you where you are?|These are hallowed grounds, leave.";
    private readonly string _aosdaChase = "I have lived a long time, I will catch you.|You dare flee like a coward?";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public AncientDragon(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: I am immortal, see you soon"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Hahahahaha"));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Tolo I móliant!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Draconic Omega")]
public class DraconicOmega : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _aosdaSayings = "Muahahah fool|I've met hatchlings fiercer than you|Trying to challenge me? Might as well be a mouse roaring at a mountain";
    private readonly string _aosdaChase = "Don't run coward|Fly, little one! The shadows suit you|Off so soon? I've barely warmed up!|Such haste! Did you leave your courage behind?|Flee now, and live to cower another day";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public DraconicOmega(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooooo"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Sweet release..        ^_^"));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ascradith Nem Tsu!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Jack Frost")]
public class JackFrost : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _aosdaSayings = "How about this!|I do not know what I am doing... help me|I feel the light";
    private readonly string _aosdaChase = "Hey, hey. Slow down, slow down|Don't run, I will turn you to Ice!|But you've came all this way!";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public JackFrost(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Thank you, Merry Christmas!"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooo..."));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: I do not control my actions!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;
        
        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("Yeti")]
public class Yeti : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _aosdaSayings = "Muahahah|I promised to give Christmas back!|I'm just borrowing it, leave me alone";
    private readonly string _aosdaChase = "Let's sing some carols|Come back, I just want a hug|I'm no Grinch, I'm a Yeti. There's a difference!";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;
    private bool _phaseOne;
    private bool _phaseTwo;
    private bool _phaseThree;

    public Yeti(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                UpdatePhases();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    private void UpdatePhases()
    {
        if (Monster.CurrentHp <= Monster.MaximumHp * 0.75 && !_phaseOne)
        {
            Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! You made Yeti Mad!"));
            var foundA = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanA", out var templateA);
            var foundB = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanB", out var templateB);
            var foundC = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanC", out var templateC);

            if (foundA) Monster.CreateFromTemplate(templateA, Monster.Map);
            if (foundB) Monster.CreateFromTemplate(templateB, Monster.Map);
            if (foundC) Monster.CreateFromTemplate(templateC, Monster.Map);

            _phaseOne = true;
        }

        if (Monster.CurrentHp <= Monster.MaximumHp * 0.50 && !_phaseTwo)
        {
            Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! You made Yeti Really Mad!"));
            var foundA = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanA", out var templateA);
            var foundB = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanB", out var templateB);
            var foundC = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanC", out var templateC);
            var foundD = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanD", out var templateD);
            var foundE = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanE", out var templateE);

            if (foundA) Monster.CreateFromTemplate(templateA, Monster.Map);
            if (foundB) Monster.CreateFromTemplate(templateB, Monster.Map);
            if (foundC) Monster.CreateFromTemplate(templateC, Monster.Map);
            if (foundD) Monster.CreateFromTemplate(templateD, Monster.Map);
            if (foundE) Monster.CreateFromTemplate(templateE, Monster.Map);

            _phaseTwo = true;
        }

        if (Monster.CurrentHp <= Monster.MaximumHp * 0.25 && !_phaseThree)
        {
            Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! Time to die!!"));
            var foundA = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanA", out var templateA);
            var foundB = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanB", out var templateB);
            var foundC = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanC", out var templateC);
            var foundD = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanD", out var templateD);
            var foundE = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanE", out var templateE);
            var foundF = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanF", out var templateF);
            var foundG = ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SnowmanG", out var templateG);

            if (foundA) Monster.CreateFromTemplate(templateA, Monster.Map);
            if (foundB) Monster.CreateFromTemplate(templateB, Monster.Map);
            if (foundC) Monster.CreateFromTemplate(templateC, Monster.Map);
            if (foundD) Monster.CreateFromTemplate(templateD, Monster.Map);
            if (foundE) Monster.CreateFromTemplate(templateE, Monster.Map);
            if (foundF) Monster.CreateFromTemplate(templateF, Monster.Map);
            if (foundG) Monster.CreateFromTemplate(templateG, Monster.Map);

            _phaseThree = true;
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooooo"));
        Task.Delay(300).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 13, Monster.Template.Level * 200);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Let it snow.. Let it snow.. let ittt..."));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Silent Night, Holy Night..."));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (!Monster.CantCast && !Monster.Aggressive)
        {
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
                {
                    Monster.BashEnabled = true;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.AbilityEnabled = true;
                    Monster.CastEnabled = true;
                    Monster.Direction = (byte)direction;
                    Monster.Turn();
                }

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}

[Script("World Boss Astrid")]
public class WorldBossBahamut : MonsterScript
{
    private Vector2 _location = Vector2.Zero;
    private readonly string _aosdaSayings = "I've met hatchlings fiercer than you|I'm going to enjoy this|Asra Leckto Moltuv, esta drakto|Don't die on me now|Endure!";
    private readonly string _aosdaChase = "Hahahaha, scared? You should be.|Come back, I just have a question|Such haste! Did you leave your courage behind?|Flee now.. live to cower another day hahaha, mortal";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public WorldBossBahamut(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
                Monster.ObjectUpdateEnabled = false;
            }
            else
            {
                Monster.ObjectUpdateEnabled = false;
            }

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public override void OnClick(WorldClient client)
    {
        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{Monster.CurrentHp}";
        var halfGone = Monster.MaximumHp * .5;

        colorA = Monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = Monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (Monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{Monster.CurrentHp}{{=s";
        }

        if (Monster.Template.Level >= client.Aisling.Level + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.All, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Monster.Name}: {{=bLike a phoenix, I will return."));
        Monster.LoadAndCastSpellScriptOnDeath("Double XP");
        Task.Delay(600).Wait();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.player.Map == Monster.Map);
            Monster.Target = recordTuple.player;
        }

        if (Monster.Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(Monster);
        }
        else
        {
            var sum = (uint)Random.Shared.Next(Monster.Template.Level * 100000, Monster.Template.Level * 200000);

            if (sum > 0)
            {
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        Monster.Remove();
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is Aisling { LoggedIn: false })
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);

        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Shhh now, let it consume you.."));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Weakling!"));
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove) return;
                if (walk) Walk();

                return;
            }

            if (Monster.Image == Monster.Template.ImageVarience)
                Bash();

            if (Monster.BashEnabled)
            {
                if (!Monster.CantAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (!Monster.CantAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (Monster.CantMove) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ahh, a warmup!"));
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var critical = (long)(Monster.MaximumHp * 0.03);
            if (Monster.CurrentHp <= critical)
            {
                Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {{=bYou fight well, now lets get serious!"));
                Monster.Image = (ushort)Monster.Template.ImageVarience;

                var objects = GetObjects(client.Aisling.Map, s => s.WithinRangeOf(client.Aisling), Get.AllButAislings).ToList();
                objects.Reverse();

                foreach (var aisling in Monster.AislingsNearby())
                {
                    if (objects.Count == 0) continue;
                    aisling.Client.SendVisibleEntities(objects);
                }
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }

        base.OnDamaged(client, dmg, source);
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IWorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            Monster.ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        Monster.CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby, blocked) in groupAttacking)
                {
                    if (player.Skulled || !player.LoggedIn || !nearby || blocked) continue;
                    if (player.Map != Monster.Map) continue;
                    Monster.Target = player;
                    // Highest dps player targeted, exit
                    break;
                }
            }
            else
            {
                if (Monster.Target == null)
                {
                    // Obtain a list of players nearby within ear shot
                    var targets = nearbyPlayers.ToList();
                    // Sort players based on highest threat
                    var topDps = targets.OrderByDescending(c => c.ThreatMeter);

                    foreach (var target in topDps)
                    {
                        if (target.Skulled || !target.LoggedIn) continue;
                        if (target.Map != Monster.Map) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            Monster.CheckTarget();
        }
        else
        {
            Monster.ClearTarget();
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
        {
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
            }
        }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        var assails = Monster.SkillScripts.Where(i => i.Skill.CanUse());

        Parallel.ForEach(assails, (s) =>
        {
            s.Skill.InUse = true;
            s.OnUse(Monster);
            {
                var readyTime = DateTime.UtcNow;
                readyTime = readyTime.AddSeconds(s.Skill.Template.Cooldown);
                readyTime = readyTime.AddMilliseconds(Monster.Template.AttackSpeed);
                s.Skill.NextAvailableUse = readyTime;
            }
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true, playerTuple.blocked), playerTuple);
            return;
        }

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    private void Walk()
    {
        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
            Monster.ThrownBack = false;

        if (Monster.Target != null)
        {
            if (Monster.Target is Aisling aisling)
            {
                if (Map.ID != aisling.Map.ID)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (!aisling.LoggedIn)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.TargetRecord.TaggedAislings.TryRemove(aisling.Serial, out _);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.MonsterGetFiveByFourRectInFront().Contains(Monster.Target))
            {
                Monster.BashEnabled = true;
                Monster.AbilityEnabled = true;
                Monster.CastEnabled = true;

                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.90)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
                }
            }
            else if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                if (Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction)) return;
                Monster.Direction = (byte)direction;
                Monster.Turn();

                if (Monster.Target is not Aisling player) return;
                Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (playerTuple.dmg, player, true, false), playerTuple);
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.AbilityEnabled = true;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                // Wander, AStar, and Standard Walk Methods
                if (Monster.Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    Monster.TargetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Area.GetPath(Monster, _location, Monster.TargetPos);

                    if (Monster.TargetPos == Vector2.Zero)
                    {
                        Monster.ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (Monster.Path.Result.Count > 0)
                    {
                        Monster.AStarPath(Monster, Monster.Path.Result);
                        if (!Monster.Path.Result.IsEmpty())
                            Monster.Path.Result.RemoveAt(0);
                    }

                    if (Monster.Path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Monster.Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                    {
                        if (Monster.Target is not Aisling player) return;
                        Monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerTuple);
                        Monster.TargetRecord.TaggedAislings.TryUpdate(player.Serial, (0, player, true, true), playerTuple);
                        return;
                    }

                    Monster.Wander();
                }
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.AbilityEnabled = false;
            Monster.CastEnabled = false;

            if (Monster.Template.PathQualifer.PathFlagIsSet(PathQualifer.Patrol))
            {
                if (Monster.Template.Waypoints == null)
                {
                    Monster.Wander();
                }
                else
                {
                    if (Monster.Template.Waypoints.Count > 0)
                        Monster.Patrol();
                    else
                        Monster.Wander();
                }
            }
            else
            {
                Monster.Wander();
            }
        }
    }

    #endregion
}