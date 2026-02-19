using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
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

    /// <summary>
    /// Overwritten walk state for shape shifter
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
    /// Overwritten to allow shape-shifting on damage
    /// </summary>
    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        try
        {
            var pct = Generator.RandomPercentPrecise();
            if (pct >= .92)
            {
                // Shape-shift to another sprite image
                if (monster.Image != (ushort)monster.Template.ImageVarience)
                {
                    monster.Image = (ushort)monster.Template.ImageVarience;

                    var objects = GetObjects(client.Aisling.Map, s => s.WithinRangeOf(client.Aisling), Get.AllButAislings).ToList();
                    objects.Reverse();

                    foreach (var aisling in monster.AislingsNearby())
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


    /// <summary>
    /// Overwritten to allow for different walking behavior while shape-shifted. 
    /// When reverted back to original form, monster will randomly walk and cast spells and abilities
    /// </summary>
    private void Walk()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive) return;
        if (monster.CantMove) return;
        if (monster.ThrownBack) return;

        if (monster.Target != null)
        {
            if (monster.Target is not Aisling aisling)
            {
                monster.Wander();
                return;
            }

            if (aisling.IsInvisible || aisling.Dead || aisling.Skulled || !aisling.LoggedIn || Map.ID != aisling.Map.ID)
            {
                monster.ClearTarget();
                monster.Wander();
                return;
            }

            if (monster.Image == monster.Template.Image)
            {
                if (monster.Target != null)
                {
                    monster.Target.GetPositionSnapshot(out var targetX, out var targetY);

                    if (monster.NextTo(targetX, targetY))
                    {
                        monster.NextToTarget();
                    }
                    else
                    {
                        monster.BeginPathFind();
                    }
                }
                else
                {
                    monster.BeginPathFind();
                }

            }
            else
            {
                monster.BashEnabled = false;
                monster.AbilityEnabled = true;
                monster.CastEnabled = true;
                monster.Wander();
                var pct = Generator.RandomPercentPrecise();
                if (pct >= .60)
                    monster.CastSpell();
            }
        }
        else
        {
            monster.BashEnabled = false;
            monster.CastEnabled = false;

            monster.PatrolIfSet();
        }
    }
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

    /// <summary>
    /// Overwritten to detonate monster on ability use
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

        var ability = monster.AbilityTimer.Update(elapsedTime);
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

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                Detonate();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        monster.UpdateTarget();
    }

    private void Detonate()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;
        if (monster.CantAttack) return;
        if (monster.Target != null)
        {
            monster.Target.GetPositionSnapshot(out var targetX, out var targetY);

            if (!monster.Facing(targetX, targetY, out var direction))
            {
                monster.Direction = (byte)direction;
                monster.Turn();
                return;
            }
        }

        if (monster.Target is not Damageable damageable) return;

        if (monster.Target is not { CurrentHp: > 1 })
        {
            if (monster.Target is not Aisling aisling) return;
            monster.TargetRecord.TaggedAislings.TryGetValue(aisling.Serial, out var playerTuple);
            monster.TargetRecord.TaggedAislings.TryUpdate(aisling.Serial, aisling, playerTuple);
            return;
        }

        var abilityAttempt = Generator.RandNumGen100();
        if (abilityAttempt <= 15) return;
        var abilityIdx = RandomNumberGenerator.GetInt32(monster.SkillScripts.Count);

        if (monster.SkillScripts[abilityIdx] is null) return;
        var skill = monster.SkillScripts[abilityIdx];
        skill.OnUse(monster);
        if (monster.Target == null) return;
        var suicide = monster.CurrentHp / .5;
        damageable.ApplyDamage(monster, (long)suicide, null, true);
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

    /// <summary>
    /// Overwritten to allow monster to alert other monsters of players nearby (including invisible)
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

            if (monster.AbilityEnabled && ability)
                SummonMonsterNearby();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        monster.UpdateTarget(false, true);
    }

    private void SummonMonsterNearby()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive) return;
        if (monster.CantCast) return;
        if (monster.Target is null) return;

        if (Generator.RandomPercentPrecise() <= 0.70) return;

        var monstersNearby = monster.MonstersOnMap();

        foreach (var ally in monstersNearby)
        {
            if (monster.WithinRangeOf(ally)) continue;

            var readyTime = DateTime.UtcNow;
            ally.Pos = new Vector2(monster.Pos.X, monster.Pos.Y);

            foreach (var player in monster.AislingsNearby())
            {
                player.Client.SendCreatureWalk(ally.Serial, new Point(ally.X, ally.Y), (Direction)ally.Direction);
            }

            ally.LastMovementChanged = readyTime;
            ally.LastPosition = new Position(ally.X, ally.Y);
            break;
        }
    }
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

    /// <summary>
    /// Overwritten to make monster stationary while firing off spells and distant type attacks
    /// </summary>
    public override void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var assail = monster.BashTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);

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

                return;
            }

            if (assail) Bash();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        monster.UpdateTarget();
    }

    /// <summary>
    /// Overwritten to execute "Gatling" skill, ignoring status effects
    /// </summary>
    private void Bash()
    {
        var monster = Monster;
        if (monster is null) return;

        if (monster.Target != null)
        {
            monster.Target.GetPositionSnapshot(out var targetX, out var targetY);

            if (!monster.Facing(targetX, targetY, out var direction))
            {
                monster.Direction = (byte)direction;
                monster.Turn();
            }
        }

        var gatling = monster.SkillScripts.FirstOrDefault(i => i.Skill.CanUse() && i.Skill.Template.Name == "Gatling");
        if (gatling?.Skill == null) return;
        var now = DateTime.UtcNow;

        gatling.Skill.InUse = true;

        try
        {
            gatling.OnUse(monster);
            gatling.Skill.NextAvailableUse = now
                .AddSeconds(gatling.Skill.Template.Cooldown)
                .AddMilliseconds(monster.Template.AttackSpeed);
        }
        finally
        {
            gatling.Skill.InUse = false;
        }
    }
}

[Script("Pirate")]
public class GeneralPirate : MonsterScript
{
    private readonly string _pirateSayings = "Arrr!|Arr! Let's sing a sea shanty.|Out'a me way!|Aye, yer sister is real nice|Gimmie yar gold!|Bet yar can't take me Bucko|Look at me funny and I'll a slit yar throat!|Scallywag!|Shiver my timbers|A watery grave for anyone who make us angry!|Arrr! Out'a me way and gimme yar gold!";
    private string[] Arggh => _pirateSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private bool _deathCry;

    public GeneralPirate(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to increase monetary rewards from Pirates and display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate: See ya next time!!!!!"));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 20, level * 300);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking and casting spells
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"Pirate: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        monster.UpdateTarget();
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate: See how you like this!!"));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.70)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
}

[Script("PirateOfficer")]
public class PirateOfficer : MonsterScript
{
    private readonly string _pirateSayings = "Yo Ho|All Hands!|Hoist the colors high!|Ye, ho! All together!|Never shall we die!|Bet yar can't take me Bucko";
    private string[] Arggh => _pirateSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private bool _deathCry;

    public PirateOfficer(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to increase monetary rewards from Pirates and display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate Officer: See ya next time!!!!!"));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 25, level * 350);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking and casting spells
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate Officer: Hahahahaha!!"));
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"Pirate Officer: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        monster.UpdateTarget();
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, "Pirate Officer: See how you like this!!"));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.70)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
}

[Script("Aosda Remnant")]
public class AosdaRemnant : MonsterScript
{
    private readonly string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|Many years, have I walked this path|We can stay here together..  Forever|Don't leave me, anything but that";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;

    public AosdaRemnant(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to "Ao Sith" remove debuffs if poisoned or vulnerable
    /// </summary>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.ObjectUpdateEnabled = true;
                monster.UpdateTarget();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsVulnerable || monster.IsPoisoned)
            {
                if (!monster.VulnerabilityWatch.IsRunning)
                    monster.VulnerabilityWatch.Start();

                if (monster.VulnerabilityWatch.Elapsed.TotalMilliseconds > 3000)
                {
                    var pos = monster.Pos;
                    var diceRoll = Generator.RandNumGen100();
                    if (diceRoll >= 80)
                        monster.SendAnimationNearby(75, new Position(pos));

                    foreach (var debuff in monster.Debuffs.Values)
                        debuff?.OnEnded(monster, debuff);

                    monster.VulnerabilityWatch.Restart();
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

    /// <summary>
    /// Overwritten to display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: ..."));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 13, level * 200);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking, casting spells, and allow to see invisible players
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Sweet.. release..."));
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
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        monster.UpdateTarget(false, true);
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Ascradith Nem Tsu!"));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.70)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
}

[Script("Aosda Hero")]
public class AosdaHero : MonsterScript
{
    private readonly string _aosdaSayings = "Why are you here?|Do you understand, that for which you walk?|The war was long, the pain.. longer|You should not go any further|This is where we make our stand!";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;

    public AosdaHero(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to "Ao Sith" remove debuffs if poisoned or vulnerable
    /// </summary>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.ObjectUpdateEnabled = true;
                monster.UpdateTarget();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsVulnerable || monster.IsPoisoned)
            {
                if (!monster.VulnerabilityWatch.IsRunning)
                    monster.VulnerabilityWatch.Start();

                if (monster.VulnerabilityWatch.Elapsed.TotalMilliseconds > 3000)
                {
                    var pos = monster.Pos;
                    var diceRoll = Generator.RandNumGen100();
                    if (diceRoll >= 80)
                        monster.SendAnimationNearby(75, new Position(pos));

                    foreach (var debuff in monster.Debuffs.Values)
                        debuff?.OnEnded(monster, debuff);

                    monster.VulnerabilityWatch.Restart();
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

    /// <summary>
    /// Overwritten to display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Thank you.."));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 13, level * 200);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking, casting spells, and allow to see invisible players
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Nooo.. I have much to do."));
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
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        monster.UpdateTarget(false, true);
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: A quick death!"));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.70)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
}

[Script("AncientDragon")]
public class AncientDragon : MonsterScript
{
    private readonly string _aosdaSayings = "Young one, do you where you are?|These are hallowed grounds, leave.|I have lived a long time, I will catch you.";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;

    public AncientDragon(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to "Ao Sith" remove debuffs if poisoned or vulnerable
    /// </summary>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.ObjectUpdateEnabled = true;
                monster.UpdateTarget();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsVulnerable || monster.IsPoisoned)
            {
                if (!monster.VulnerabilityWatch.IsRunning)
                    monster.VulnerabilityWatch.Start();

                if (monster.VulnerabilityWatch.Elapsed.TotalMilliseconds > 3000)
                {
                    var pos = monster.Pos;
                    var diceRoll = Generator.RandNumGen100();
                    if (diceRoll >= 80)
                        monster.SendAnimationNearby(75, new Position(pos));

                    foreach (var debuff in monster.Debuffs.Values)
                        debuff?.OnEnded(monster, debuff);

                    monster.VulnerabilityWatch.Restart();
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

    /// <summary>
    /// Overwritten to display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: I am immortal, see you soon..."));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 13, level * 200);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking, casting spells, and allow to see invisible players
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Hahahahaha"));
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
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        monster.UpdateTarget(false, true);
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Tolo I móliant!"));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.70)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
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

    /// <summary>
    /// Overwritten to summon additional rats when approached - only once per spawn
    /// </summary>
    public override void OnApproach(WorldClient client)
    {
        if (client == null) return;
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.SwarmOnApproach) return;
        monster.SwarmOnApproach = true;

        Task.Delay(500).Wait();

        // Per-map monster template cache
        if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(client.Aisling.Map.ID, out var templates) || templates.Length == 0) return;
        templates.TryGetValue(t => t.Name == "SRat0", out var rat);

        for (var i = 0; i < Random.Shared.Next(1, 2); i++)
        {
            var summoned = Monster.Create(rat, monster.Map);
            if (summoned == null) return;
            summoned.X = monster.X + Random.Shared.Next(0, 4);
            summoned.Y = monster.Y + Random.Shared.Next(0, 4);
            summoned.Direction = monster.Direction;
            AddObject(summoned);
        }
    }
}

[Script("Death Beetle Swarm")]
public class DbSwarm : MonsterScript
{
    public DbSwarm(Monster monster, Area map) : base(monster, map)
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

    /// <summary>
    /// Overwritten to summon additional beetles when approached - only once per spawn
    /// </summary>
    public override void OnApproach(WorldClient client)
    {
        if (client == null) return;
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.SwarmOnApproach) return;
        monster.SwarmOnApproach = true;

        Task.Delay(500).Wait();

        // Per-map monster template cache
        if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(client.Aisling.Map.ID, out var templates) || templates.Length == 0) return;
        templates.TryGetValue(t => t.Name == "SkSpS1", out var rat);

        for (var i = 0; i < Random.Shared.Next(1, 2); i++)
        {
            var summoned = Monster.Create(rat, monster.Map);
            if (summoned == null) return;
            summoned.X = monster.X + Random.Shared.Next(0, 4);
            summoned.Y = monster.Y + Random.Shared.Next(0, 4);
            summoned.Direction = monster.Direction;
            AddObject(summoned);
        }
    }
}

[Script("ChaosHydraLava")]
public class ChaosHydraLava : MonsterScript
{
    private readonly string _aosdaSayings = "Teine remembers all who burn… and all who scream.|You step into my fire. Is mise an tine.|My heads speak with one voice, bás i ngach lasair.|Stone fears me. Water flees me. Flesh feeds me.";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;
    private bool _phaseOne;
    private bool _phaseTwo;
    private bool _phaseThree;

    public ChaosHydraLava(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to "Ao Sith" remove debuffs if poisoned or vulnerable; Adds UpdatePhases() 
    /// to spawn adds at 75%, 50%, and 25% health thresholds
    /// </summary>
    public override void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.ObjectUpdateEnabled = true;
                monster.UpdateTarget();
                UpdatePhases();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsVulnerable || monster.IsPoisoned)
            {
                if (!monster.VulnerabilityWatch.IsRunning)
                    monster.VulnerabilityWatch.Start();

                if (monster.VulnerabilityWatch.Elapsed.TotalMilliseconds > 3000)
                {
                    var pos = monster.Pos;
                    var diceRoll = Generator.RandNumGen100();
                    if (diceRoll >= 80)
                        monster.SendAnimationNearby(75, new Position(pos));

                    foreach (var debuff in monster.Debuffs.Values)
                        debuff?.OnEnded(monster, debuff);

                    monster.VulnerabilityWatch.Restart();
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
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.CurrentHp <= monster.MaximumHp * 0.75 && !_phaseOne)
        {
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: Every age ends in fire. I am merely patient."));
            monster.Image = 206; // Two-headed hydra
            monster.DefenseElement = ElementManager.Element.Sorrow;
            monster.UpdateAddAndRemove();
            _phaseOne = true;
        }

        if (monster.CurrentHp <= monster.MaximumHp * 0.50 && !_phaseTwo)
        {
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: The world was forged once. I am what remains."));
            monster.Image = 315; // One-headed hydra
            monster.OffenseElement = ElementManager.Element.Sorrow;
            monster.UpdateAddAndRemove();
            _phaseTwo = true;
        }

        if (monster.CurrentHp <= monster.MaximumHp * 0.25 && !_phaseThree)
        {
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: Fire is not chaos. Fire is memory."));
            monster.OffenseElement = ElementManager.Element.Rage;
            monster.DefenseElement = ElementManager.Element.Rage;
            _phaseThree = true;
        }
    }

    /// <summary>
    /// Overwritten to display a death message
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Remember my name when the fire takes you."));

        Task.Delay(300).Wait();

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
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 13, level * 200);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to allow commentary while walking, casting spells, and allow to see invisible players
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
            if (monster.Target.IsWeakened && !_deathCry)
            {
                _deathCry = true;
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Enough. Éiríonn an tine fiáin!"));
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
                CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
        {
            monster.PreWalkChecks();
            var rand = Generator.RandomPercentPrecise();
            if (rand >= 0.93)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
            }
        }

        monster.UpdateTarget(false, true);
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: One head watches. One judges. One burns."));

        var scripts = monster.SpellScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0) return;

        if (Generator.RandomPercentPrecise() < 0.50)
            return;

        var idx = RandomNumberGenerator.GetInt32(scripts.Count);
        var script = scripts[idx];
        script?.OnUse(monster, target);
    }
}
