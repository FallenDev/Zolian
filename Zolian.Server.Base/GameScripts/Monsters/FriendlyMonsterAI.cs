using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("BaseFriendly")]
public class BaseFriendlyMonster : MonsterScript
{
    public BaseFriendlyMonster(Monster monster, Area map) : base(monster, map)
    {
        Monster.ObjectUpdateTimer.Delay = TimeSpan.FromMilliseconds(500);
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
        Monster.Summoned = true;

        if (!Monster.TimeTillDead.IsRunning)
            Monster.TimeTillDead.Start();
    }

    /// <summary>
    /// Overwritten to control various summoned monster states and behaviors
    /// </summary>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Summoner == null)
        {
            OnDeath();
            return;
        }

        if (monster.TimeTillDead.Elapsed.TotalMinutes > 3)
        {
            OnDeath();
            return;
        }

        if (monster.Summoner.CurrentMapId != monster.CurrentMapId)
            OnDeath();

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.Summoner.SendAnimationNearby(171, null, monster.Serial);
                monster.ObjectUpdateEnabled = true;
                UpdateTarget();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsConfused || monster.IsFrozen || monster.IsStopped || monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    /// <summary>
    /// Overwritten to control friendly information
    /// </summary>
    public override void OnClick(WorldClient client)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{monster.CurrentHp}";
        var halfGone = monster.MaximumHp * .5;

        colorA = monster.OffenseElement switch
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

        colorB = monster.DefenseElement switch
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

        if (monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{monster.CurrentHp}{{=s";
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.SealedAc} {{=aWill: {{=s{monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
    }

    /// <summary>
    /// Overwritten to drop a corpse on death
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

        if (monster.Summoner != null)
        {
            var corpse = new Item();
            corpse = corpse.Create(monster, "Corpse");
            corpse.Release(monster, monster.Position);
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to control friendly monster state
    /// </summary>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Target is not null && monster.Template.EngagedWalkingSpeed > 0)
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.EngagedWalkingSpeed);

        var assail = monster.BashTimer.Update(elapsedTime);
        var ability = monster.AbilityTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);
        var walk = monster.WalkTimer.Update(elapsedTime);

        if (monster.Target is Monster target)
        {
            if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && target.IsWeakened)
            {
                monster.Aggressive = false;
                monster.ClearTarget();
            }

            if (target.IsInvisible || target.Skulled)
            {
                if (!monster.WalkEnabled) return;

                if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                {
                    monster.Aggressive = false;
                    monster.ClearTarget();
                }

                if (monster.CantMove) return;
                if (walk) Walk();

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
            Walk();

        monster.UpdateTarget();
    }

    /// <summary>
    /// Overwritten to target monsters nearby instead of players
    /// </summary>
    private void UpdateTarget()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;
        if (!monster.ObjectUpdateEnabled) return;
        if (!monster.Aggressive) return;

        var nearbyPlayers = monster.MonstersNearby().Where(m => m.Template.SpawnType != SpawnQualifer.Summoned).ToList();

        if (monster.Target is Monster target)
        {
            if (target.Template.SpawnType == SpawnQualifer.Summoned)
                monster.SummonedClearTarget();
        }

        if (nearbyPlayers.Count == 0)
        {
            monster.SummonedClearTarget();
        }

        if (monster.Target is null || !monster.Target.Alive)
            monster.Target = nearbyPlayers.RandomIEnum();

        if (monster.Target != null) return;
        monster.SummonedCheckTarget();
        monster.Target = monster.Summoner.Target;
    }

    /// <summary>
    /// Overwritten to control friendly monster movement
    /// </summary>
    private void Walk()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;
        if (monster.ThrownBack) return;

        if (monster.Target != null)
        {
            if (monster.Target is Monster targetMonster)
            {
                if (targetMonster.IsInvisible || targetMonster.Skulled)
                {
                    Monster.Wander();
                    return;
                }
            }

            if (monster.Target != null && monster.NextTo((int)monster.Target.Pos.X, (int)monster.Target.Pos.Y))
            {
                if (monster.Facing((int)monster.Target.Pos.X, (int)monster.Target.Pos.Y, out var direction))
                {
                    monster.BashEnabled = true;
                    monster.AbilityEnabled = true;
                    monster.CastEnabled = true;
                }
                else
                {
                    monster.BashEnabled = false;
                    monster.AbilityEnabled = true;
                    monster.CastEnabled = true;
                    monster.Direction = (byte)direction;
                    monster.Turn();
                }
            }
            else
            {
                monster.BashEnabled = false;
                monster.CastEnabled = true;
                if (monster.WalkTo((int)monster.Target.Pos.X, (int)monster.Target.Pos.Y)) return;
                monster.Wander();
            }
        }
        else
        {
            monster.BashEnabled = false;
            monster.CastEnabled = false;
            monster.Wander();
        }
    }
}