using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Numerics;
using System.Security.Cryptography;

namespace Darkages.GameScripts.Monsters;

[Script("Shape Shifter")]
public class ShapeShifter : MonsterScript
{
    public ShapeShifter(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

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

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
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

        Monster.UpdateTarget();
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

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

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
            if (Monster.Target is not Aisling aisling)
            {
                Monster.Wander();
                return;
            }

            if (aisling.IsInvisible || aisling.Dead || aisling.Skulled || !aisling.LoggedIn || Map.ID != aisling.Map.ID)
            {
                Monster.ClearTarget();
                Monster.Wander();
                return;
            }

            if (Monster.Image == Monster.Template.Image)
            {
                if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
                {
                    Monster.NextToTarget();
                }
                else
                {
                    Monster.BeginPathFind();
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

            Monster.PatrolIfSet();
        }
    }

    #endregion
}

[Script("Self Destruct")]
public class SelfDestruct : MonsterScript
{
    public SelfDestruct(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
        }

        Monster.UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

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

        if (Monster.Target is not Damageable damageable) return;

        if (Monster.Target is not { CurrentHp: > 1 })
        {
            if (Monster.Target is not Aisling aisling) return;
            Monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            Monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, aisling, playerTuple);
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
        damageable.ApplyDamage(Monster, (long)suicide, null, true);
        OnDeath();
    }
}

[Script("Alert Summon")]
public class AlertSummon : MonsterScript
{
    public AlertSummon(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

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

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
        }

        Monster.UpdateTarget();
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
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void SummonMonsterNearby()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;

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

    #endregion
}

[Script("Turret")]
public class Turret : MonsterScript
{
    public Turret(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        var assail = Monster.BashTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                return;
            }

            if (assail) Bash();

            if (Monster.CastEnabled)
            {
                if (!Monster.CantCast)
                    if (cast) CastSpell();
            }
        }

        Monster.UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
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
        gatling.OnCleanup();
        gatling.Skill.InUse = false;
    }

    private void CastSpell()
    {
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

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
    private readonly string _pirateSayings = "Arrr!|Arr! Let's sing a sea shanty.|Out'a me way!|Aye, yer sister is real nice|Gimmie yar gold!|Bet yar can't take me Bucko|Look at me funny and I'll a slit yar throat!|Scallywag!|Shiver my timbers|A watery grave for anyone who make us angry!|Arrr! Out'a me way and gimme yar gold!";
    private readonly string _pirateChase = "Ya landlubber can't run from us!|Harhar!! Running away eh?|Time fer a plundering!";
    private string[] Arggh => _pirateSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] Arrrgh => _pirateChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private int RunCount => Arrrgh.Length;
    private bool _deathCry;

    public GeneralPirate(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        Monster.UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("PirateOfficer")]
public class PirateOfficer : MonsterScript
{
    private readonly string _pirateSayings = "Yo Ho|All Hands!|Hoist the colors high!|Ye, ho! All together!|Never shall we die!|Bet yar can't take me Bucko";
    private readonly string _pirateChase = "It will be the brig for ye!|All hands! We have a stowaway!|Haha!!";
    private string[] Arggh => _pirateSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] Arrrgh => _pirateChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private int RunCount => Arrrgh.Length;
    private bool _deathCry;

    public PirateOfficer(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        Monster.UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Aosda Remnant")]
public class AosdaRemnant : MonsterScript
{
    private readonly string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|Many years, have I walked this path";
    private readonly string _aosdaChase = "Come back, we can stay here together..  Forever|Don't leave me, anything but that";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public AosdaRemnant(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget(false, true);
            }

            Monster.ObjectUpdateEnabled = false;

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooooo"));
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Sweet release..        ^_^"));
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        Monster.UpdateTarget(false, true);
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ascradith Nem Tsu!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Aosda Hero")]
public class AosdaHero : MonsterScript
{
    private readonly string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|The war was long, the pain.. longer";
    private readonly string _aosdaChase = "You should not go any further|This is where we make our stand!";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public AosdaHero(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget();
            }

            Monster.ObjectUpdateEnabled = false;

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooo.. I have much to do."));
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooo.. I have much to do."));
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        Monster.UpdateTarget();
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 30) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: A quick death!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.50)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("AncientDragon")]
public class AncientDragon : MonsterScript
{
    private readonly string _aosdaSayings = "Young one, do you where you are?|These are hallowed grounds, leave.";
    private readonly string _aosdaChase = "I have lived a long time, I will catch you.|You dare flee like a coward?";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public AncientDragon(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget(false, true);
            }

            Monster.ObjectUpdateEnabled = false;

            if (Monster.IsVulnerable || Monster.IsPoisoned)
            {
                var pos = Monster.Pos;
                foreach (var debuff in Monster.Debuffs.Values)
                {
                    debuff?.OnEnded(Monster, debuff);
                    Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, new Position(pos)));
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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: I am immortal, see you soon"));
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Aisling aisling)
        {
            if (Monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Hahahahaha"));
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
            var rand = Generator.RandomNumPercentGen();
            if (rand >= 0.93)
            {
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        Monster.UpdateTarget(false, true);
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Tolo I móliant!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Swarm")]
public class Swarm : MonsterScript
{
    public Swarm(Monster monster, Area map) : base(monster, map)
    {
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
                Monster.UpdateTarget(true);
            }

            Monster.ObjectUpdateEnabled = false;

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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.TargetRecord.TaggedAislings.IsEmpty &&
            Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);
        else
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);

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

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

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
            if (walk) Monster.Walk();
        }

        Monster.UpdateTarget(true);
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        Monster.MonsterBank.Add(item);
    }

    private string LevelColor(WorldClient client)
    {
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Template.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Template.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Template.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Template.Level}{{=s" : $"{{=q{Monster.Template.Level}{{=s";
    }

    public override void OnApproach(WorldClient client)
    {
        if (Monster.SwarmOnApproach) return;
        Monster.SwarmOnApproach = true;
        Task.Delay(500).Wait();
        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("SRat0", out var rat);

        for (var i = 0; i < Random.Shared.Next(1, 2); i++)
        {
            var summoned = Monster.Create(rat, Monster.Map);
            if (summoned == null) return;
            summoned.X = Monster.X + Random.Shared.Next(0, 4);
            summoned.Y = Monster.Y + Random.Shared.Next(0, 4);
            summoned.Direction = Monster.Direction;
            AddObject(summoned);
        }
    }

    #region Actions

    private void Bash()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.SkillScripts.Count == 0) return;

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
            s.OnCleanup();
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 60) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(Monster.AbilityScripts.Count);

        if (Monster.AbilityScripts[abilityIdx] is null) return;
        Monster.AbilityScripts[abilityIdx].OnUse(Monster);
        Monster.AbilityScripts[abilityIdx].OnCleanup();
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}