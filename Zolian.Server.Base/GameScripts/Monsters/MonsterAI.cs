using System.Numerics;
using System.Security.Cryptography;

using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

using ServiceStack;

namespace Darkages.GameScripts.Monsters;

[Script("Common")]
public class BaseMonsterIntelligence : MonsterScript
{
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    public BaseMonsterIntelligence(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = new List<Item>();
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
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        if (!aisling.IsInvisible) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is invisible, dying, not logged in or nearby; Ignore them
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is invisible, dying, or not logged in; Ignore them
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    public WeakCommon(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = new List<Item>();
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
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        if (!aisling.IsInvisible) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }
        
        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on lowest threat
                var groupAttacking = tagged.OrderBy(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is invisible, dying, not logged in or nearby; Ignore them
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is invisible, dying, or not logged in; Ignore them
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Lowest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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

[Script("Pirate")]
public class GeneralPirate : MonsterScript
{
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    private string _pirateSayings = "Arrr!|Arr! Let's sing a sea shanty.|Out'a me way!|Aye, yer sister is real nice|Gimmie yar gold!|Bet yar can't take me Bucko|Look at me funny and I'll a slit yar throat!|Scallywag!|Shiver my timbers|A watery grave for anyone who make us angry!|Arrr! Out'a me way and gimme yar gold!";
    private string _pirateChase = "Ya landlubber can't run from us!|Harhar!! Running away eh?|Time fer a plundering!";
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
        Monster.MonsterBank = new List<Item>();
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
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));
        Task.Delay(300).Wait();
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        if (!aisling.IsInvisible) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is invisible, dying, not logged in or nearby; Ignore them
                    if (player.IsInvisible || player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is invisible, dying, or not logged in; Ignore them
                        if (target.IsInvisible || target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
                Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
        {
            Monster.ThrownBack = false;
            return;
        }

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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arrrgh[RandomNumberGenerator.GetInt32(RunCount + 1) % Arrrgh.Length]}"));
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    public ShadowSight(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = new List<Item>();
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
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is dying, not logged in or nearby; Ignore them
                    if (player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is dying, or not logged in; Ignore them
                        if (target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    public WeakShadowSight(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(1500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = new List<Item>();
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
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on lowest threat
                var groupAttacking = tagged.OrderBy(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is dying, not logged in or nearby; Ignore them
                    if (player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is dying, or not logged in; Ignore them
                        if (target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Lowest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;

    private string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|Many years, have I walked this path";
    private string _aosdaChase = "Come back, we can stay here together..  Forever|Don't leave me, anything but that";
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
        Monster.MonsterBank = new List<Item>();
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
                    Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
                }
            }

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.Logger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            Crashes.TrackError(e);
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(WorldClient client = null)
    {
        Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooooo"));
        Task.Delay(300).Wait();
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position, false);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Monster.Target is null)
        {
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault();
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
                if (Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold))
                    Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
            }
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
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
                Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Sweet release..        ^_^"));
            }

            if (aisling.Skulled || aisling.Dead)
            {
                if (!Monster.WalkEnabled) return;
                if (Monster.CantMove || Monster.Blind) return;
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
            if (Monster.CantMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(WorldClient client) { }

    public override void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
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

    #region Targeting

    private void CheckTarget()
    {
        if (Monster.Target is not Aisling aisling) return;
        if (!aisling.Skulled && aisling.LoggedIn) return;
        Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        Monster.Target = null;
    }

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            Monster.Path?.Result?.Clear();
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        var nearbyPlayers = Monster.AislingsEarShotNearby().ToList();

        if (nearbyPlayers.Count == 0)
        {
            ClearTarget();
            Monster.TargetRecord.TaggedAislings.Clear();
        }

        CheckTarget();

        if (Monster.Aggressive)
        {
            // Cache of players attacking monster
            var tagged = Monster.TargetRecord.TaggedAislings.Values;

            if (!tagged.IsEmpty())
            {
                // Sort group based on highest threat
                var groupAttacking = tagged.OrderByDescending(c => c.player.ThreatMeter);

                foreach (var (_, player, nearby) in groupAttacking)
                {
                    // If a player is dying, not logged in or nearby; Ignore them
                    if (player.Skulled || !player.LoggedIn || !nearby) continue;
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
                        // If a player is dying, or not logged in; Ignore them
                        if (target.Skulled || !target.LoggedIn) continue;
                        Monster.Target = target;
                        // Highest dps player targeted, exit
                        break;
                    }
                }
            }

            CheckTarget();
        }
        else
        {
            ClearTarget();
        }
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (Monster.Target != null)
            if (!Monster.Facing((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }

        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
        if (!Monster.Target.WithinRangeOf(Monster)) return;

        Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ascradith Nem Tsu!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, (0, aisling, true), playerTuple);
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
                Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        if (Monster.CantMove) return;
        if (Monster.ThrownBack)
        {
            Monster.ThrownBack = false;
            return;
        }

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

            if (Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
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
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
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
                    _targetPos = new Vector2(Monster.Target.Pos.X, Monster.Target.Pos.Y);
                    Monster.Path = Monster.Map.GetPath(Monster, _location, _targetPos);

                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
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

                    if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;

                    Monster.TargetRecord.TaggedAislings.TryRemove(Monster.Target.Serial, out _);
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