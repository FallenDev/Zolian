using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Security.Cryptography;

namespace Darkages.GameScripts.Monsters;

[Script("Common")]
public class BaseMonsterIntelligence : MonsterScript
{
    public BaseMonsterIntelligence(Monster monster, Area map) : base(monster, map)
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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel  + 30)
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
    }

    private void CastSpell()
    {
        if (Monster.CantCast) return;
        if (Monster.Target is null) return;
        if (!Monster.Target.WithinMonsterSpellRangeOf(Monster)) return;

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomPercentPrecise() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Weak Common")]
public class WeakCommon : MonsterScript
{
    public WeakCommon(Monster monster, Area map) : base(monster, map)
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

            if (Monster.BashEnabled || Monster.NextToTargetFirstAttack)
            {
                if (!Monster.CantAttack)
                    if (assail || Monster.NextToTargetFirstAttack) Bash();
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

    #region Actions

    private void Bash()
    {
        Monster.NextToTargetFirstAttack = false;
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
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() <= 0.70) return;
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

        if (Generator.RandomPercentPrecise() <= 0.70) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Inanimate")]
public class Inanimate : MonsterScript
{
    public Inanimate(Monster monster, Area map) : base(monster, map)
    {
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
            var recordTuple = Monster.TargetRecord.TaggedAislings.Values.FirstOrDefault(p => p.Map == Monster.Map);
            Monster.Target = recordTuple;
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
    }


    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            lock (Monster.TaggedAislingsLock)
            {
                var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);

                if (!tagged)
                    Monster.TryAddPlayerAndHisGroup(client.Aisling);
                else
                    Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, client.Aisling, client.Aisling);
            }
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

[Script("ShadowSight")]
public class ShadowSight : MonsterScript
{
    public ShadowSight(Monster monster, Area map) : base(monster, map)
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

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

                return;
            }

            if (Monster.BashEnabled || Monster.NextToTargetFirstAttack)
            {
                if (!Monster.CantAttack)
                    if (assail || Monster.NextToTargetFirstAttack) Bash();
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
        Monster.NextToTargetFirstAttack = false;
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
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() <= 0.70) return;
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

        if (Generator.RandomPercentPrecise() <= 0.70) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Weak ShadowSight")]
public class WeakShadowSight : MonsterScript
{
    public WeakShadowSight(Monster monster, Area map) : base(monster, map)
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
                Monster.UpdateTarget(true, true);
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

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    Monster.Aggressive = false;

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Monster.Walk();

                return;
            }

            if (Monster.BashEnabled || Monster.NextToTargetFirstAttack)
            {
                if (!Monster.CantAttack)
                    if (assail || Monster.NextToTargetFirstAttack) Bash();
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

        Monster.UpdateTarget(true, true);
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
        Monster.NextToTargetFirstAttack = false;
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
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() <= 0.70) return;
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

        if (Generator.RandomPercentPrecise() <= 0.70) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("RiftMob")]
public class RiftMob : MonsterScript
{
    public RiftMob(Monster monster, Area map) : base(monster, map)
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

        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel  + 30)
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
            Monster.GenerateRiftRewards(aisling);
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

            if (Monster.BashEnabled || Monster.NextToTargetFirstAttack)
            {
                if (!Monster.CantAttack)
                    if (assail || Monster.NextToTargetFirstAttack) Bash();
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
        Monster.NextToTargetFirstAttack = false;
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
            s.Skill.InUse = false;
        });
    }

    private void Ability()
    {
        if (Monster.CantAttack) return;
        // Training Dummy or other enemies who can't attack
        if (Monster.AbilityScripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() <= 0.70) return;
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

        if (Generator.RandomPercentPrecise() <= 0.70) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}