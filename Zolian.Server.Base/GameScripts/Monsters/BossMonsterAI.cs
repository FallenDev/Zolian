using System.Security.Cryptography;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Draconic Omega")]
public class DraconicOmega : MonsterScript
{
    private readonly string _aosdaSayings = "Muahahah fool|I've met hatchlings fiercer than you|Trying to challenge me? Might as well be a mouse roaring at a mountain|Such haste! Did you leave your courage behind?|Flee now, and live to cower another day";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;

    public DraconicOmega(Monster monster, Area map) : base(monster, map)
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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: I am Omega, I am immorta..."));

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
            var sum = (uint)Random.Shared.Next(level * 30, level * 500);

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
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Hahhahaa fools"));
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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Ascradith Nem!!!!"));

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

[Script("Jack Frost")]
public class JackFrost : MonsterScript
{
    private readonly string _aosdaSayings = "How about this!|I do not know what I am doing... help me|I feel the light|Hey, hey. Slow down, slow down";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;

    public JackFrost(Monster monster, Area map) : base(monster, map)
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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Thank you! Merry Christmas!!"));

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
            var sum = (uint)Random.Shared.Next(level * 30, level * 500);

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
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: I'm melting..."));
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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: I do not control my actions!"));

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

[Script("Yeti")]
public class Yeti : MonsterScript
{
    private readonly string _aosdaSayings = "Muahahah|I promised to give Christmas back!|I'm just borrowing it, leave me alone|Let's sing some carols|Come back, I just want a hug|I'm no Grinch, I'm a Yeti. There's a difference!";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private bool _deathCry;
    private bool _phaseOne;
    private bool _phaseTwo;
    private bool _phaseThree;

    public Yeti(Monster monster, Area map) : base(monster, map)
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
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: AHHHHH That Hurts! You made Yeti Mad!"));
            // Per-map monster template cache
            if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(monster.Map.ID, out var templates) || templates.Length == 0) return;
            var foundA = templates.TryGetValue(t => t.Name == "SnowmanA", out var templateA);
            var foundB = templates.TryGetValue(t => t.Name == "SnowmanB", out var templateB);
            var foundC = templates.TryGetValue(t => t.Name == "SnowmanC", out var templateC);

            if (foundA)
            {
                var mob1 = Monster.Create(templateA, monster.Map);
                if (mob1 is not null)
                    ObjectManager.AddObject(mob1);
            }
            if (foundB)
            {
                var mob2 = Monster.Create(templateB, monster.Map);
                if (mob2 is not null)
                    ObjectManager.AddObject(mob2);
            }
            if (foundC)
            {
                var mob3 = Monster.Create(templateC, monster.Map);
                if (mob3 is not null)
                    ObjectManager.AddObject(mob3);
            }

            _phaseOne = true;
        }

        if (monster.CurrentHp <= monster.MaximumHp * 0.50 && !_phaseTwo)
        {
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: AHHHHH That Hurts! You made Yeti Really Mad!"));
            // Per-map monster template cache
            if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(monster.Map.ID, out var templates) || templates.Length == 0) return;
            var foundA = templates.TryGetValue(t => t.Name == "SnowmanA", out var templateA);
            var foundB = templates.TryGetValue(t => t.Name == "SnowmanB", out var templateB);
            var foundC = templates.TryGetValue(t => t.Name == "SnowmanC", out var templateC);
            var foundD = templates.TryGetValue(t => t.Name == "SnowmanD", out var templateD);
            var foundE = templates.TryGetValue(t => t.Name == "SnowmanE", out var templateE);

            if (foundA)
            {
                var mob1 = Monster.Create(templateA, monster.Map);
                if (mob1 is not null)
                    ObjectManager.AddObject(mob1);
            }
            if (foundB)
            {
                var mob2 = Monster.Create(templateB, monster.Map);
                if (mob2 is not null)
                    ObjectManager.AddObject(mob2);
            }
            if (foundC)
            {
                var mob3 = Monster.Create(templateC, monster.Map);
                if (mob3 is not null)
                    ObjectManager.AddObject(mob3);
            }
            if (foundD)
            {
                var mob4 = Monster.Create(templateD, monster.Map);
                if (mob4 is not null)
                    ObjectManager.AddObject(mob4);
            }
            if (foundE)
            {
                var mob5 = Monster.Create(templateE, monster.Map);
                if (mob5 is not null)
                    ObjectManager.AddObject(mob5);
            }

            _phaseTwo = true;
        }

        if (monster.CurrentHp <= monster.MaximumHp * 0.25 && !_phaseThree)
        {
            monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Shout, $"{monster.Name}: AHHHHH That Hurts! Time to die!!"));
            // Per-map monster template cache
            if (!ServerSetup.Instance.MonsterTemplateByMapCache.TryGetValue(monster.Map.ID, out var templates) || templates.Length == 0) return;
            var foundA = templates.TryGetValue(t => t.Name == "SnowmanA", out var templateA);
            var foundB = templates.TryGetValue(t => t.Name == "SnowmanB", out var templateB);
            var foundC = templates.TryGetValue(t => t.Name == "SnowmanC", out var templateC);
            var foundD = templates.TryGetValue(t => t.Name == "SnowmanD", out var templateD);
            var foundE = templates.TryGetValue(t => t.Name == "SnowmanE", out var templateE);
            var foundF = templates.TryGetValue(t => t.Name == "SnowmanF", out var templateF);
            var foundG = templates.TryGetValue(t => t.Name == "SnowmanG", out var templateG);

            if (foundA)
            {
                var mob1 = Monster.Create(templateA, monster.Map);
                if (mob1 is not null)
                    ObjectManager.AddObject(mob1);
            }
            if (foundB)
            {
                var mob2 = Monster.Create(templateB, monster.Map);
                if (mob2 is not null)
                    ObjectManager.AddObject(mob2);
            }
            if (foundC)
            {
                var mob3 = Monster.Create(templateC, monster.Map);
                if (mob3 is not null)
                    ObjectManager.AddObject(mob3);
            }
            if (foundD)
            {
                var mob4 = Monster.Create(templateD, monster.Map);
                if (mob4 is not null)
                    ObjectManager.AddObject(mob4);
            }
            if (foundE)
            {
                var mob5 = Monster.Create(templateE, monster.Map);
                if (mob5 is not null)
                    ObjectManager.AddObject(mob5);
            }
            if (foundF)
            {
                var mob6 = Monster.Create(templateF, monster.Map);
                if (mob6 is not null)
                    ObjectManager.AddObject(mob6);
            }
            if (foundG)
            {
                var mob7 = Monster.Create(templateG, monster.Map);
                if (mob7 is not null)
                    ObjectManager.AddObject(mob7);
            }

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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: There was Donner, and Blitzen..."));

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
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Let it snow.. Let it snow.. let it snow"));
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

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Silent Night, Holy Night..."));

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

[Script("World Boss Astrid")]
public class WorldBossBahamut : MonsterScript
{
    private readonly string _aosdaSayings = "I've met hatchlings fiercer than you|I'm going to enjoy this|Asra Leckto Moltuv, esta drakto|Don't die on me now|Endure!";
    private readonly string _aosdaChase = "Hahahaha, scared? You should be.|Come back, I just have a question|Such haste! Did you leave your courage behind?|Flee now.. live to cower another day hahaha, mortal";
    private string[] GhostChat => _aosdaSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public WorldBossBahamut(Monster monster, Area map) : base(monster, map)
    {
        Monster.CastTimer.RandomizedVariance = 60;
        Monster.AbilityTimer.RandomizedVariance = 50;
        Monster.MonsterBank = [];
    }

    /// <summary>
    /// Overwritten to "Ao Sith" remove debuffs if poisoned, vulnerable, and silenced
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

            if (monster.IsVulnerable || monster.IsPoisoned || monster.IsSilenced)
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
    /// Overwritten to display a death message and cast double xp on death
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.All, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{monster.Name}: {{=bLike a phoenix, I will return."));
        monster.LoadAndCastSpellScriptOnDeath("Double XP");

        Task.Delay(600).Wait();

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
        else
        {
            var item = new Item();
            item = item.Create(monster, "Bahamut's Treasure Chest");
            item?.Release(monster, monster.Position);
        }

        if (monster.Target is Aisling aisling)
        {
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 1000, level * 2000);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    /// <summary>
    /// Overwritten to detect invisible, enable berserk, target weakest, and display deathcry commentary
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
                monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Shhh now, let it consume you.."));
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) Walk();

                return;
            }

            // Berserk
            if (monster.Image == monster.Template.ImageVarience)
                Bash();

            if (monster.BashEnabled && assail && !monster.CantAttack)
                Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            Walk();

        monster.UpdateTarget(true, true);
    }

    /// <summary>
    /// Overwritten to display approach message
    /// </summary>
    public override void OnApproach(WorldClient client)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Ahh, a warmup!"));
    }

    /// <summary>
    /// Overwritten to change apperance at 3% health and enable berserk mode
    /// </summary>
    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        try
        {
            var critical = (long)(monster.MaximumHp * 0.03);
            if (monster.CurrentHp <= critical)
            {
                monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {{=bYou fight well, now lets get serious!"));
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
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }

        base.OnDamaged(client, dmg, source);
    }

    /// <summary>
    /// Overwritten to allow distant facing checks
    /// </summary>
    private void Bash()
    {
        var monster = Monster;
        if (monster is null || monster.CantAttack) return;

        if (monster.Target != null)
        {
            monster.Target.GetPositionSnapshot(out var targetX, out var targetY);

            if (Monster.NextTo(targetX, targetY))
            {
                if (!monster.Facing(targetX, targetY, out var direction))
                {
                    monster.Direction = (byte)direction;
                    monster.Turn();
                }
            }
        }

        var scripts = monster.SkillScripts;

        // Training Dummy or other enemies who can't attack
        if (scripts.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var attackSpeedMs = monster.Template.AttackSpeed;

        for (var i = 0; i < scripts.Count; i++)
        {
            var s = scripts[i];
            if (s is null)
                continue;

            var skill = s.Skill;
            if (skill is null)
                continue;

            if (!skill.CanUse())
                continue;

            skill.InUse = true;

            try
            {
                s.OnUse(monster);

                skill.NextAvailableUse = now
                    .AddSeconds(skill.Template.Cooldown)
                    .AddMilliseconds(attackSpeedMs);
            }
            finally
            {
                skill.InUse = false;
            }
        }
    }

    /// <summary>
    /// Overwritten to allow commentary while walking, and adjust targets based off of a wide-spread frontal cone attack
    /// </summary>
    private void Walk()
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.ThrownBack)
            monster.ThrownBack = false;

        if (monster.Target != null)
        {
            if (monster.Target is not Aisling aisling)
            {
                monster.Wander();
                return;
            }

            if (aisling.Dead || aisling.Skulled || !aisling.LoggedIn || Map.ID != aisling.Map.ID)
            {
                monster.ClearTarget();
                monster.Wander();
                return;
            }

            monster.Target.GetPositionSnapshot(out var targetX, out var targetY);

            if (monster.GetFiveByFourRectInFront().Contains(monster.Target))
            {
                monster.BashEnabled = true;
                monster.AbilityEnabled = true;
                monster.CastEnabled = true;

                var rand = Generator.RandomPercentPrecise();
                if (rand >= 0.90)
                {
                    monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
                }
            }
            else if (monster.Target != null && monster.NextTo(targetX, targetY))
            {
                monster.NextToTarget();
            }
            else
            {
                monster.AbilityEnabled = true;
                var rand = Generator.RandomPercentPrecise();
                if (rand >= 0.80)
                {
                    monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                monster.BeginPathFind();
            }
        }
        else
        {
            monster.BashEnabled = false;
            monster.AbilityEnabled = false;
            monster.CastEnabled = false;
            monster.PatrolIfSet();
        }
    }
}

[Script("BB Shade")]
public class BBShade : MonsterScript
{
    private readonly string _pirateSayings = "Yo Ho|All Hands!|Hoist the colors high!|Ye, ho! All together!|Never shall we die!|Dead men tell no tales..";
    private string[] Arggh => _pirateSayings.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private bool _deathCry;

    public BBShade(Monster monster, Area map) : base(monster, map)
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
    /// Overwritten to display death message, and issue "chest" reward
    /// </summary>
    public override void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: See ya next time!!!!!"));

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
        else
        {
            var item = new Item();
            item = item.Create(monster, "Strong Treasure Chest");
            item?.Release(monster, monster.Position);
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
    /// Overwritten to allow commentary while walking and casting spells, and see Invisible players
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

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
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
            if (rand >= 0.80)
            {
                monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: {Arggh[RandomNumberGenerator.GetInt32(Count + 1) % Arggh.Length]}"));
            }
        }

        Monster.UpdateTarget(false, true);
    }

    private void CastSpell()
    {
        var monster = Monster;
        if (monster is null || monster.CantCast)
            return;

        var target = monster.Target;
        if (target is null) return;

        if (!target.WithinMonsterSpellRangeOf(monster)) return;

        monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(monster.Serial, PublicMessageType.Normal, $"{monster.Name}: Dead men tell no tales!!"));

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