using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Security.Cryptography;

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

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster is null) return;
        if (!Monster.IsAlive) return;
        if (Monster.Summoner == null)
        {
            OnDeath();
            return;
        }

        if (Monster.TimeTillDead.Elapsed.TotalMinutes > 3)
        {
            OnDeath();
            return;
        }

        if (Monster.Summoner.CurrentMapId != Monster.CurrentMapId)
            OnDeath();

        var update = Monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                Monster.Summoner.SendAnimationNearby(171, null, Monster.Serial);
                Monster.ObjectUpdateEnabled = true;
                UpdateTarget();
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

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{Monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{Monster.Size} {{=aAC: {{=s{Monster.SealedAc} {{=aWill: {{=s{Monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{Monster.OffenseElement} {{=aD: {colorB}{Monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aSummoner: {Monster.Summoner.Username}");
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

        if (Monster.Summoner != null)
        {
            var corpse = new Item();
            corpse = corpse.Create(Monster, "Corpse");
            corpse.Release(Monster, Monster.Position);
        }

        Monster.Remove();
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        if (Monster.Target is not null && Monster.Template.EngagedWalkingSpeed > 0)
            Monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(Monster.Template.EngagedWalkingSpeed);

        var assail = Monster.BashTimer.Update(elapsedTime);
        var ability = Monster.AbilityTimer.Update(elapsedTime);
        var cast = Monster.CastTimer.Update(elapsedTime);
        var walk = Monster.WalkTimer.Update(elapsedTime);

        if (Monster.Target is Monster monster)
        {
            if (Monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && Monster.Target.IsWeakened)
            {
                Monster.Aggressive = false;
                Monster.ClearTarget();
            }

            if (monster.IsInvisible || monster.Skulled)
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
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return $"{{=n{Monster.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{Monster.Level}{{=s";
        if (Monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{Monster.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{Monster.Level}{{=s";
        if (Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{Monster.Level}{{=s";
        return Monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{Monster.Level}{{=s" : $"{{=q{Monster.Level}{{=s";
    }

    private void UpdateTarget()
    {
        if (!Monster.ObjectUpdateEnabled) return;
        if (!Monster.Aggressive) return;

        var nearbyPlayers = Monster.MonstersNearby().Where(m => m.Template.SpawnType != SpawnQualifer.Summoned).ToList();

        if (Monster.Target is Monster target)
        {
            if (target.Template.SpawnType == SpawnQualifer.Summoned)
                Monster.SummonedClearTarget();
        }

        if (nearbyPlayers.Count == 0)
        {
            Monster.SummonedClearTarget();
        }

        if (Monster.Target is null || !Monster.Target.Alive)
            Monster.Target = nearbyPlayers.RandomIEnum();

        if (Monster.Target != null) return;
        Monster.SummonedCheckTarget();
        Monster.Target = Monster.Summoner.Target;
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
            if (Monster.Target is Monster monster)
            {
                if (monster.IsInvisible || monster.Skulled)
                {
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
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = true;
                if (Monster.WalkTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y)) return;
                Monster.Wander();
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.CastEnabled = false;
            Monster.Wander();
        }
    }

    #endregion
}