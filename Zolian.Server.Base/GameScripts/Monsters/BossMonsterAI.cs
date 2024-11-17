using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using ServiceStack;

using System.Numerics;
using System.Security.Cryptography;

namespace Darkages.GameScripts.Monsters;

[Script("Draconic Omega")]
public class DraconicOmega : MonsterScript
{
    private readonly string _aosdaSayings = "Muahahah fool|I've met hatchlings fiercer than you|Trying to challenge me? Might as well be a mouse roaring at a mountain";
    private readonly string _aosdaChase = "Don't run coward|Fly, little one! The shadows suit you|Off so soon? I've barely warmed up!|Such haste! Did you leave your courage behind?|Flee now, and live to cower another day";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public DraconicOmega(Monster monster, Area map) : base(monster, map)
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

[Script("Jack Frost")]
public class JackFrost : MonsterScript
{
    private readonly string _aosdaSayings = "How about this!|I do not know what I am doing... help me|I feel the light";
    private readonly string _aosdaChase = "Hey, hey. Slow down, slow down|Don't run, I will turn you to Ice!|But you've came all this way!";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public JackFrost(Monster monster, Area map) : base(monster, map)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Thank you, Merry Christmas!"));
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
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Nooooo..."));
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

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: I do not control my actions!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("Yeti")]
public class Yeti : MonsterScript
{
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
                UpdatePhases();
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

    private void UpdatePhases()
    {
        if (Monster.CurrentHp <= Monster.MaximumHp * 0.75 && !_phaseOne)
        {
            Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! You made Yeti Mad!"));
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
            Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! You made Yeti Really Mad!"));
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
            Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Shout, $"{Monster.Name}: AHHHHH That Hurts! Time to die!!"));
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
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Let it snow.. Let it snow.. let ittt..."));
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

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Silent Night, Holy Night..."));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}

[Script("World Boss Astrid")]
public class WorldBossBahamut : MonsterScript
{
    private readonly string _aosdaSayings = "I've met hatchlings fiercer than you|I'm going to enjoy this|Asra Leckto Moltuv, esta drakto|Don't die on me now|Endure!";
    private readonly string _aosdaChase = "Hahahaha, scared? You should be.|Come back, I just have a question|Such haste! Did you leave your courage behind?|Flee now.. live to cower another day hahaha, mortal";
    private string[] GhostChat => _aosdaSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] GhostChase => _aosdaChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => GhostChat.Length;
    private int RunCount => GhostChase.Length;
    private bool _deathCry;

    public WorldBossBahamut(Monster monster, Area map) : base(monster, map)
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
        Monster.SendTargetedClientMethod(PlayerScope.All, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Monster.Name}: {{=bLike a phoenix, I will return."));
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
                Monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Shhh now, let it consume you.."));
            }

            if (aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                Monster.ClearTarget();

                if (Monster.CantMove || !Monster.WalkEnabled) return;
                if (walk) Walk();

                return;
            }

            if (Monster.Image == Monster.Template.ImageVarience)
            {
                Bash();
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

        Monster.UpdateTarget(true, true);
    }

    public override void OnApproach(WorldClient client)
    {
        Monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: Ahh, a warmup!"));
    }

    public override void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var critical = (long)(Monster.MaximumHp * 0.03);
            if (Monster.CurrentHp <= critical)
            {
                Monster.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {{=bYou fight well, now lets get serious!"));
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
            if (Monster.Target is not Aisling aisling)
            {
                Monster.Wander();
                return;
            }

            if (aisling.Dead || aisling.Skulled || !aisling.LoggedIn || Map.ID != aisling.Map.ID)
            {
                Monster.ClearTarget();
                Monster.Wander();
                return;
            }

            if (Monster.MonsterGetFiveByFourRectInFront().Contains(Monster.Target))
            {
                Monster.BashEnabled = true;
                Monster.AbilityEnabled = true;
                Monster.CastEnabled = true;

                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.90)
                {
                    Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChat[RandomNumberGenerator.GetInt32(Count + 1) % GhostChat.Length]}"));
                }
            }
            else if (Monster.Target != null && Monster.NextTo((int)Monster.Target.Pos.X, (int)Monster.Target.Pos.Y))
            {
                Monster.NextToTarget();
            }
            else
            {
                Monster.AbilityEnabled = true;
                var rand = Generator.RandomNumPercentGen();
                if (rand >= 0.80)
                {
                    Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Name}: {GhostChase[RandomNumberGenerator.GetInt32(RunCount + 1) % GhostChase.Length]}"));
                }

                Monster.BeginPathFind();
            }
        }
        else
        {
            Monster.BashEnabled = false;
            Monster.AbilityEnabled = false;
            Monster.CastEnabled = false;

            Monster.PatrolIfSet();
        }
    }

    #endregion
}

[Script("BB Shade")]
public class BBShade : MonsterScript
{
    private readonly string _pirateSayings = "Yo Ho|All Hands!|Hoist the colors high!|Ye, ho! All together!|Never shall we die!";
    private readonly string _pirateChase = "Dead men tell no tales...|If ye be brave or fool enough to face me|Haha!!";
    private string[] Arggh => _pirateSayings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private string[] Arrrgh => _pirateChase.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private int Count => Arggh.Length;
    private int RunCount => Arrrgh.Length;
    private bool _deathCry;

    public BBShade(Monster monster, Area map) : base(monster, map)
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
        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Template.BaseName}: See ya next time!!!!!"));
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
                Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Template.BaseName}: Hahahahaha!!"));
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

        Monster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(Monster.Serial, PublicMessageType.Normal, $"{Monster.Template.BaseName}: Dead men tell no tales!!"));

        // Training Dummy or other enemies who can't attack
        if (Monster.SpellScripts.Count == 0) return;

        if (!(Generator.RandomNumPercentGen() >= 0.70)) return;
        var spellIdx = RandomNumberGenerator.GetInt32(Monster.SpellScripts.Count);

        if (Monster.SpellScripts[spellIdx] is null) return;
        Monster.SpellScripts[spellIdx].OnUse(Monster, Monster.Target);
    }

    #endregion
}