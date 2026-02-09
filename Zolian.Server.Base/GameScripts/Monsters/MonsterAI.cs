using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

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

    /// <summary>
    /// Overwritten to target weaker players
    /// </summary>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Target is not null && monster.TargetRecord.TaggedAislings.IsEmpty &&
            monster.Template.EngagedWalkingSpeed > 0)
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.EngagedWalkingSpeed);
        else
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.MovementSpeed);

        var assail = monster.BashTimer.Update(elapsedTime);
        var ability = monster.AbilityTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);
        var walk = monster.WalkTimer.Update(elapsedTime);

        if (monster.Target is Aisling aisling)
        {
            if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && monster.Target.IsWeakened)
            {
                monster.Aggressive = false;
                monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    monster.Aggressive = false;

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        Monster.UpdateTarget(true);
    }
}

[Script("Inanimate")]
public class Inanimate : MonsterScript
{
    public Inanimate(Monster monster, Area map) : base(monster, map)
    {
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to prevent inanimate monsters from updating
    /// </summary>
    public override void Update(TimeSpan elapsedTime) { }

    /// <summary>
    /// Overwritten to display basic on-click information
    /// </summary>
    public override void OnClick(WorldClient client) => client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aHP: {Monster.CurrentHp}/{Monster.MaximumHp}");

    /// <summary>
    /// Overwritten to prevent players from changing monster to "Aggressive"
    /// </summary>
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
}

[Script("Loot Goblin")]
public class LootGoblin : MonsterScript
{
    public LootGoblin(Monster monster, Area map) : base(monster, map)
    {
        Monster.MonsterBank = [];
        Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.MovementSpeed);
    }

    /// <summary>
    /// Overwritten to continously update loot goblin's state
    /// </summary>
    /// <param name="elapsedTime"></param>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        try
        {
            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    /// <summary>
    /// Overwritten to display basic on-click information
    /// </summary>
    public override void OnClick(WorldClient client)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}!!!!");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{Monster.Template.BaseName}!!!!");
    }

    /// <summary>
    /// Overwritten to continously update loot goblin's "Wander" state
    /// </summary>
    /// <param name="elapsedTime"></param>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var walk = monster.WalkTimer.Update(elapsedTime);
        if (monster.WalkEnabled && walk)
            monster.Wander();
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

    /// <summary>
    /// Overwritten to allow monster to target invisible players
    /// </summary>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Target is not null && monster.TargetRecord.TaggedAislings.IsEmpty &&
            monster.Template.EngagedWalkingSpeed > 0)
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.EngagedWalkingSpeed);
        else
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.MovementSpeed);

        var assail = monster.BashTimer.Update(elapsedTime);
        var ability = monster.AbilityTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);
        var walk = monster.WalkTimer.Update(elapsedTime);

        if (monster.Target is Aisling aisling)
        {
            if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && monster.Target.IsWeakened)
            {
                monster.Aggressive = false;
                monster.ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    monster.Aggressive = false;

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        monster.UpdateTarget(false, true);
    }
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

    /// <summary>
    /// Overwritten to allow monster to target invisible and weak players
    /// </summary>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Target is not null && monster.TargetRecord.TaggedAislings.IsEmpty &&
            monster.Template.EngagedWalkingSpeed > 0)
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.EngagedWalkingSpeed);
        else
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.MovementSpeed);

        var assail = monster.BashTimer.Update(elapsedTime);
        var ability = monster.AbilityTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);
        var walk = monster.WalkTimer.Update(elapsedTime);

        if (monster.Target is Aisling aisling)
        {
            if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && monster.Target.IsWeakened)
            {
                monster.Aggressive = false;
                monster.ClearTarget();
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    monster.Aggressive = false;

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        monster.UpdateTarget(true, true);
    }
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

    /// <summary>
    /// Overwritten to drop loot from rift rewards and offer a higher gold drop chance
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        var bank = monster.MonsterBank;
        for (var i = 0; i < bank.Count; i++)
        {
            var item = bank[i];
            if (item is null)
                continue;

            item.Release(monster, monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
                item.ShowTo(player);
        }

        if (monster.Target is null)
        {
            Aisling found = null;

            foreach (var kvp in monster.TargetRecord.TaggedAislings)
            {
                var p = kvp.Value;
                if (p?.Map == monster.Map)
                {
                    found = p;
                    break;
                }
            }

            monster.Target = found;
        }

        if (monster.Target is Aisling aisling)
        {
            monster.GenerateRiftRewards(aisling);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 25, level * 600);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }
}