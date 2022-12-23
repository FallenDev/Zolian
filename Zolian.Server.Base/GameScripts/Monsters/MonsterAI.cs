using System.Numerics;
using System.Security.Cryptography;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

namespace Darkages.GameScripts.Monsters;

[Script("Common Monster")]
public class MonsterBaseIntelligence : MonsterScript
{
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;
    private Task<List<Vector2>> _path;
    private Sprite Target => Monster.Target;

    public MonsterBaseIntelligence(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(ServerSetup.Instance.Config.GlobalBaseSkillDelay);
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

    public override void OnClick(GameClient client)
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

        client.SendMessage(0x03, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendMessage(0x03, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(GameClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.AggroList.Remove(Monster.Target.Serial);
        }
        else
        {
            if (!Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold)) return;

            var sum = (uint)Random.Shared.Next((int)(Monster.Template.Level * 13), (int)(Monster.Template.Level * 200));

            if (sum > 0)
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Target is Aisling { LoggedIn: false })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
        }

        if (Target is not null && Monster.TaggedAislings.Count > 0 && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        // ToDo: Game Master Nullify damage
        if (Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Target.IsWeakened)
            {
                Monster.Aggressive = false;
                ClearTarget();
            }

            if (aisling.Invisible || aisling.Skulled || aisling.Dead/*|| aisling.GameMaster*/)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (!Monster.CanMove || Monster.Blind) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (Monster.CanAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (Monster.CanAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (Monster.CanCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (!Monster.CanMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(GameClient client) { }

    public override void OnLeave(GameClient client)
    {
        Monster.AggroList.Remove(client.Aisling.Serial);
        if (Monster.Target == client.Aisling) ClearTarget();
    }

    public override void OnDamaged(GameClient client, long dmg, Sprite source)
    {
        try
        {
            Monster.DamageReceived += dmg;

            if (!Monster.AggroList.Contains(client.Aisling.Serial))
                Monster.AggroList.Add(client.Aisling.Serial);
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }

        Monster.Aggressive = true;
    }

    public override void OnItemDropped(GameClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;

        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IGameClient client)
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
        if (Monster.Template.Level <= client.Aisling.Level - 10)
            return $"{{=i{Monster.Template.Level}{{=s";
        return $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Targeting

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            _path?.Result?.Clear();
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

        if (Monster.Target is Aisling checkTarget)
        {
            if (checkTarget.Skulled || checkTarget.Invisible)
            {
                Monster.AggroList.Remove(Monster.Target.Serial);
                Monster.Target = null;
            }
        }

        if (Monster.Aggressive)
        {
            var targets = GetObjects(Map, i => i.WithinRangeOf(Monster), Get.MonsterDamage);

            foreach (var target in targets)
            {
                if (target is Aisling { Skulled: true }) continue;
                if (target is not Aisling aisling) continue;
                if (aisling.Invisible) continue;
                if (!Monster.AggroList.Contains(aisling.Serial))
                    Monster.AggroList.Add(aisling.Serial);

                foreach (var targetSerial in Monster.AggroList.Where(serial => aisling.Serial == serial))
                {
                    // if target is null, make player the target
                    Monster.Target ??= aisling;

                    if (targetSerial == Monster.Target.Serial) continue;
                    if (Monster.Target is not Aisling currentTarget) continue;
                    if (aisling.ThreatMeter <= currentTarget.ThreatMeter) continue;

                    // if target is greater than current target's threat, then make target
                    Monster.Target = aisling;
                }
            }
        }

        if (Target is not null) return;
        if (Monster.Template.MoodType == MoodQualifer.Neutral |
            Monster.Template.MoodType == MoodQualifer.Idle)
        {
            Monster.Aggressive = false;
        }

        ClearTarget();
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (!Monster.CanAttack) return;
        if (Target != null)
            if (!Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }
        if (Monster.SkillScripts.Count == 0) return;

        if (Target is not { CurrentHp: > 1 })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
            return;
        }

        foreach (var skillScript in Monster.SkillScripts.Where(i => i.Skill.CanUse()))
        {
            skillScript.OnUse(Monster);
            {
                skillScript.Skill.InUse = true;
                var readyTime = DateTime.Now;
                if (skillScript.Skill.Template.Cooldown > 0)
                {
                    skillScript.Skill.NextAvailableUse = readyTime.AddSeconds(skillScript.Skill.Template.Cooldown);
                }
                else
                {
                    skillScript.Skill.NextAvailableUse = readyTime.AddMilliseconds(
                        Monster.Template.AttackSpeed > 0
                            ? Monster.Template.AttackSpeed
                            : 1500);
                }
            }
        }

        foreach (var skillScript in Monster.SkillScripts.Where(i => i.Skill.CanUse()))
        {
            skillScript.Skill.InUse = false;
        }
    }

    private void Ability()
    {
        if (!Monster.CanAttack) return;
        if (Target != null)
            if (!Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }
        if (Monster.AbilityScripts.Count == 0) return;

        if (Target is not { CurrentHp: > 1 })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
            return;
        }

        if (Monster.AbilityScripts.Count == 0) return;
        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is not null)
            Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (!Monster.CanCast) return;
        if (!Monster.CanAttack) return;
        if (Target is null) return;
        if (!Target.WithinRangeOf(Monster)) return;
        if (Monster.SpellScripts.Count == 0) return;
        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is not null)
            Monster.SpellScripts[spellIdx].OnUse(Monster, Target);
    }

    private void Walk()
    {
        if (!Monster.CanMove) return;
        if (Monster.ThrownBack) return;

        if (Target != null)
        {
            if (Map.ID != Target.Map.ID)
            {
                Monster.AggroList.Remove(Monster.Target.Serial);
                Monster.Wander();
                return;
            }

            if (Monster.Target is Aisling aisling)
            {
                if (!aisling.LoggedIn)
                {
                    Monster.AggroList.Remove(Monster.Target.Serial);
                    Monster.Wander();
                    return;
                }

                if (aisling.Invisible || aisling.Dead || aisling.Skulled)
                {
                    Monster.AggroList.Remove(Monster.Target.Serial);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.NextTo((int)Target.Pos.X, (int)Target.Pos.Y))
            {
                if (Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
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
                if (Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    _targetPos = new Vector2(Target.Pos.X, Target.Pos.Y);
                    _path = Monster.Map.GetPath(Monster, _location, _targetPos);
                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (_path.Result.Count > 0)
                        Monster.AStarPath(Monster, _path.Result);

                    if (_path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Target.Pos.X, (int)Target.Pos.Y)) return;

                    Monster.AggroList.Remove(Monster.Target.Serial);
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

[Script("ShadowSight Monster")]
public class MonsterShadowSight : MonsterScript
{
    private Vector2 _targetPos = Vector2.Zero;
    private Vector2 _location = Vector2.Zero;
    private Task<List<Vector2>> _path;
    private Sprite Target => Monster.Target;

    public MonsterShadowSight(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(ServerSetup.Instance.Config.GlobalBaseSkillDelay);
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

    public override void OnClick(GameClient client)
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

        client.SendMessage(0x03, $"{{=c{Monster.Template.BaseName} {{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.Ac}");
        client.SendMessage(0x03, $"{{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp} {{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
    }

    public override void OnDeath(GameClient client = null)
    {
        Monster.Remove();

        foreach (var item in Monster.MonsterBank.Where(item => item != null))
        {
            item.Release(Monster, Monster.Position);
            item.AddObject(item);

            foreach (var player in item.AislingsNearby())
            {
                item.ShowTo(player);
            }
        }

        if (Target is Aisling aisling)
        {
            Monster.GenerateRewards(aisling);
            Monster.AggroList.Remove(Monster.Target.Serial);
        }
        else
        {
            if (!Monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold)) return;

            var sum = (uint)Random.Shared.Next((int)(Monster.Template.Level * 13), (int)(Monster.Template.Level * 200));

            if (sum > 0)
                Money.Create(Monster, sum, new Position(Monster.Pos.X, Monster.Pos.Y));
        }

        ServerSetup.Instance.GlobalMonsterCache.TryRemove(Monster.Serial, out _);
        DelObject(Monster);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Target is Aisling { LoggedIn: false })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
        }

        if (Target is not null && Monster.TaggedAislings.Count > 0 && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        // ToDo: Game Master Nullify damage
        if (Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Target.IsWeakened)
            {
                Monster.Aggressive = false;
                ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead/*|| aisling.GameMaster*/)
            {
                if (!Monster.WalkEnabled) return;

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    Monster.Aggressive = false;
                    ClearTarget();
                }

                if (!Monster.CanMove || Monster.Blind) return;
                if (walk) Walk();

                return;
            }

            if (Monster.BashEnabled)
            {
                if (Monster.CanAttack)
                    if (assail) Bash();
            }

            if (Monster.AbilityEnabled)
            {
                if (Monster.CanAttack)
                    if (ability) Ability();
            }

            if (Monster.CastEnabled)
            {
                if (Monster.CanCast)
                    if (cast) CastSpell();
            }
        }

        if (Monster.WalkEnabled)
        {
            if (!Monster.CanMove || Monster.Blind) return;
            if (walk) Walk();
        }

        if (Monster.Target != null) return;
        UpdateTarget();
    }

    public override void OnApproach(GameClient client) { }

    public override void OnLeave(GameClient client)
    {
        Monster.AggroList.Remove(client.Aisling.Serial);
        if (Monster.Target == client.Aisling) ClearTarget();
    }

    public override void OnDamaged(GameClient client, long dmg, Sprite source)
    {
        try
        {
            Monster.DamageReceived += dmg;

            if (!Monster.AggroList.Contains(client.Aisling.Serial))
                Monster.AggroList.Add(client.Aisling.Serial);
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.ToString());
            Crashes.TrackError(ex);
        }

        Monster.Aggressive = true;
    }

    public override void OnItemDropped(GameClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;

        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(IGameClient client)
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
        if (Monster.Template.Level <= client.Aisling.Level - 10)
            return $"{{=i{Monster.Template.Level}{{=s";
        return $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Targeting

    private void ClearTarget()
    {
        Monster.CastEnabled = false;
        Monster.BashEnabled = false;
        Monster.WalkEnabled = true;

        if (Monster.Target is Aisling)
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
        }

        Monster.Target = null;
        _targetPos = Vector2.Zero;

        try
        {
            _path?.Result?.Clear();
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

        if (Monster.Target is Aisling { Skulled: true })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
            Monster.Target = null;
        }

        if (Monster.Aggressive)
        {
            var targets = GetObjects(Map, i => i.WithinRangeOf(Monster), Get.MonsterDamage);

            foreach (var target in targets)
            {
                if (target is Aisling { Skulled: true }) continue;
                if (target is not Aisling aisling) continue;
                if (!Monster.AggroList.Contains(aisling.Serial))
                    Monster.AggroList.Add(aisling.Serial);

                foreach (var targetSerial in Monster.AggroList.Where(serial => aisling.Serial == serial))
                {
                    // if target is null, make player the target
                    Monster.Target ??= aisling;

                    if (targetSerial == Monster.Target.Serial) continue;
                    if (Monster.Target is not Aisling currentTarget) continue;
                    if (aisling.ThreatMeter <= currentTarget.ThreatMeter) continue;

                    // if target is greater than current target's threat, then make target
                    Monster.Target = aisling;
                }
            }
        }

        if (Target is not null) return;
        if (Monster.Template.MoodType == MoodQualifer.Neutral |
            Monster.Template.MoodType == MoodQualifer.Idle)
        {
            Monster.Aggressive = false;
        }

        ClearTarget();
    }

    #endregion

    #region Actions

    private void Bash()
    {
        if (!Monster.CanAttack) return;
        if (Target != null)
            if (!Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }
        if (Monster.SkillScripts.Count == 0) return;

        if (Target is not { CurrentHp: > 1 })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
            return;
        }

        foreach (var skillScript in Monster.SkillScripts.Where(i => i.Skill.CanUse()))
        {
            skillScript.OnUse(Monster);
            {
                skillScript.Skill.InUse = true;
                var readyTime = DateTime.Now;
                if (skillScript.Skill.Template.Cooldown > 0)
                {
                    skillScript.Skill.NextAvailableUse = readyTime.AddSeconds(skillScript.Skill.Template.Cooldown);
                }
                else
                {
                    skillScript.Skill.NextAvailableUse = readyTime.AddMilliseconds(
                        Monster.Template.AttackSpeed > 0
                            ? Monster.Template.AttackSpeed
                            : 1500);
                }
            }
        }

        foreach (var skillScript in Monster.SkillScripts.Where(i => i.Skill.CanUse()))
        {
            skillScript.Skill.InUse = false;
        }
    }

    private void Ability()
    {
        if (!Monster.CanAttack) return;
        if (Target != null)
            if (!Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
            {
                Monster.Direction = (byte)direction;
                Monster.Turn();
                return;
            }
        if (Monster.AbilityScripts.Count == 0) return;

        if (Target is not { CurrentHp: > 1 })
        {
            Monster.AggroList.Remove(Monster.Target.Serial);
            return;
        }

        if (Monster.AbilityScripts.Count == 0) return;
        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is not null)
            Monster.AbilityScripts[abilityIdx].OnUse(Monster);
    }

    private void CastSpell()
    {
        if (!Monster.CanCast) return;
        if (!Monster.CanAttack) return;
        if (Target is null) return;
        if (!Target.WithinRangeOf(Monster)) return;
        if (Monster.SpellScripts.Count == 0) return;
        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is not null)
            Monster.SpellScripts[spellIdx].OnUse(Monster, Target);
    }

    private void Walk()
    {
        if (!Monster.CanMove) return;
        if (Monster.ThrownBack) return;

        if (Target != null)
        {
            if (Map.ID != Target.Map.ID)
            {
                Monster.AggroList.Remove(Monster.Target.Serial);
                Monster.Wander();
                return;
            }

            if (Monster.Target is Aisling aisling)
            {
                if (!aisling.LoggedIn)
                {
                    Monster.AggroList.Remove(Monster.Target.Serial);
                    Monster.Wander();
                    return;
                }

                if (aisling.Dead || aisling.Skulled)
                {
                    Monster.AggroList.Remove(Monster.Target.Serial);
                    Monster.Wander();
                    return;
                }
            }

            if (Monster.NextTo((int)Target.Pos.X, (int)Target.Pos.Y))
            {
                if (Monster.Facing((int)Target.Pos.X, (int)Target.Pos.Y, out var direction))
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
                if (Target == null | !Monster.Aggressive)
                {
                    Monster.Wander();
                }
                else
                {
                    Monster.AStar = true;
                    _location = new Vector2(Monster.Pos.X, Monster.Pos.Y);
                    _targetPos = new Vector2(Target.Pos.X, Target.Pos.Y);
                    _path = Monster.Map.GetPath(Monster, _location, _targetPos);
                    if (Monster.ThrownBack) return;

                    if (_targetPos == Vector2.Zero)
                    {
                        ClearTarget();
                        Monster.Wander();
                        return;
                    }

                    if (_path.Result.Count > 0)
                        Monster.AStarPath(Monster, _path.Result);

                    if (_path.Result.Count != 0) return;
                    Monster.AStar = false;

                    if (Target == null)
                    {
                        Monster.Wander();
                        return;
                    }

                    if (Monster.WalkTo((int)Target.Pos.X, (int)Target.Pos.Y)) return;

                    Monster.AggroList.Remove(Monster.Target.Serial);
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