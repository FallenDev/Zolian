using System.Security.Cryptography;

using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Rescue")]
public class Rescue(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Rescue";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter += aisling.Str * 100000;
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy)
            {
                if (i is not Damageable damageable) continue;
                if (damageable is Aisling { Skulled: true } savedAisling)
                {
                    savedAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                    if (debuff != null)
                    {
                        debuff.Cancelled = true;
                        debuff.OnEnded(savedAisling, debuff);
                    }

                    savedAisling.Client.Revive();
                }

                if (damageable.HasDebuff("Beag Suain"))
                    damageable.Debuffs.TryRemove("Beag Suain", out _);

                if (damageable.HasDebuff("Silence"))
                    damageable.Debuffs.TryRemove("Silence", out _);

                damageable.ApplyDamage(damageDealer, 0, Skill);
                damageable.SendAnimationNearby(Skill.Template.TargetAnimation, null, damageable.Serial);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!Skill.CanUse()) return;

        var success = Generator.RandNumGen100();

        if (success <= 5)
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Wind Blade")]
public class Wind_Blade(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Wind Blade";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 40 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Dex * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Beag Suain")]
public class Beag_Suain(Skill skill) : SkillScript(skill)
{
    private Sprite _target;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Beag Suain";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                _target = enemy;
                OnFailed(sprite);
                return;
            }

            var debuff = new DebuffBeagsuain();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, 0, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }
}

[Script("Vampiric Slash")]
public class Vampiric_Slash(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Vampiric Slash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(2);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                damageDealer.CurrentHp += (int)dmgCalc;
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 60 + Skill.Level;
            dmg = client.Aisling.Int * 5 + client.Aisling.Wis * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Charge")]
public class Charge(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Charge";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        if (_target == null)
        {
            OnFailed(sprite);
            return;
        }

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var dmgCalc = DamageCalc(damageDealer);
            var position = _target.Position;
            var mapCheck = damageDealer.Map.ID;
            var wallPosition = damageDealer.GetPendingChargePosition(7, damageDealer);
            var targetPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, position);
            var wallPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, wallPosition);

            if (mapCheck != damageDealer.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (damageDealer.Direction)
                {
                    case 0:
                        position.Y++;
                        break;
                    case 1:
                        position.X--;
                        break;
                    case 2:
                        position.Y--;
                        break;
                    case 3:
                        position.X++;
                        break;
                }

                if (damageDealer.Position != position)
                {
                    GlobalSkillMethods.Step(damageDealer, position.X, position.Y);
                }

                if (_target is not Damageable damageable) return;
                damageable.ApplyDamage(damageDealer, dmgCalc, Skill);
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, _target.Serial);

                if (!_crit) return;
                damageDealer.SendAnimationNearby(387, null, sprite.Serial);
            }
            else
            {
                GlobalSkillMethods.Step(damageDealer, wallPosition.X, wallPosition.Y);
                var stunned = new DebuffBeagsuain();
                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, stunned, damageDealer, Skill);
                damageDealer.SendAnimationNearby(208, null, damageDealer.Serial);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
        _enemyList?.Clear();
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
        {
            OnFailed(aisling);
            return;
        }

        Target(aisling);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = Skill.Level * 2;
            dmg = client.Aisling.Str * 5 + client.Aisling.Con * 4;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5 + damageMonster.Con * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Damageable damageable) return;

        try
        {
            _enemyList = damageable.DamageableGetInFront(7);
            _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                var mapCheck = damageable.Map.ID;
                var wallPosition = damageable.GetPendingChargePositionNoTarget(3, damageable);
                var wallPos = GlobalSkillMethods.DistanceTo(damageable.Position, wallPosition);

                if (mapCheck != damageable.Map.ID) return;
                if (!(wallPos > 0)) OnFailed(damageable);

                if (damageable.Position != wallPosition)
                {
                    GlobalSkillMethods.Step(damageable, wallPosition.X, wallPosition.Y);
                }

                if (wallPos <= 2)
                {
                    var stunned = new DebuffBeagsuain();
                    GlobalSkillMethods.ApplyPhysicalDebuff(damageable, stunned, damageable, Skill);
                    damageable.SendAnimationNearby(208, null, damageable.Serial);
                }

                if (damageable is Aisling skillUsed)
                    skillUsed.UsedSkill(Skill);
            }
            else
            {
                OnSuccess(sprite);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Beag Suain Ia Gar")]
public class Beag_Suain_Ia_Gar(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate.");
        GlobalSkillMethods.OnFailed(sprite, Skill, sprite);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Beag Suain Ia Gar";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var list = damageDealer.MonstersNearby();

            if (list.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var target in list)
            {
                var debuff = new DebuffBeagsuaingar();
                var chance = Generator.RandomNumPercentGen();
                if (chance <= 0.85)
                    GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, target, Skill);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }
}

[Script("Raise Threat")]
public class Raise_Threat(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        GlobalSkillMethods.OnFailed(sprite, Skill, sprite);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Raise Threat";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter *= 4;
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemies = damageDealer.MonstersNearby();

            foreach (var monster in enemies.Where(e => e is { IsAlive: true }))
            {
                monster.Target = damageDealer;
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }
}

[Script("Draconic Leash")]
public class Draconic_Leash(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        GlobalSkillMethods.OnFailed(sprite, Skill, sprite);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Draconic Leash";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var monstersNearby = damageDealer.MonstersNearby();
            var monsters = monstersNearby
                .Where(mSprite => !mSprite.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)).Where(mSprite =>
                    !mSprite.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)).ToList();

            if (monsters.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            var monster = monsters.RandomIEnum();

            if (monster != null)
            {
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, monster.Position);
                monster.Pos = damageDealer.Pos;
                monster.UpdateAddAndRemove();
            }

            damageDealer.SendAnimationNearby(139, null, damageDealer.Serial);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Taunt")]
public class Taunt(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed.");
        GlobalSkillMethods.OnFailed(sprite, Skill, sprite);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Taunt";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter += aisling.Str * 1000000;
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 70,
            BodyAnimation = BodyAnimation.Tears,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Aisling damageDealer) return;
            var targets = damageDealer.GetInFrontToSide();

            if (targets.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var target in targets)
            {
                if (target is not Monster monster) continue;
                lock (monster.TaggedAislingsLock)
                {
                    monster.TargetRecord.TaggedAislings.Clear();
                    monster.TryAddPlayerAndHisGroup(damageDealer);
                }

                monster.Target = damageDealer;
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, monster.Position);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}

[Script("Briarthorn Aura")]
public class Briarthorn(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, sprite);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Briarthorns";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        if (sprite is not Damageable damageDealer) return;

        // Remove other Auras
        var hasLawAura = damageDealer.Buffs.TryGetValue("Laws of Aosda", out var laws);
        if (hasLawAura)
            laws.OnEnded(damageDealer, laws);

        var buff = new aura_BriarThorn();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Damageable damageable) return;
        if (damageable.HasBuff("Briarthorn Aura"))
        {
            OnFailed(sprite);
            return;
        }

        OnSuccess(damageable);
    }
}

[Script("Laws of Aosda")]
public class LawsOfAosda(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, sprite);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Laws of Aosda";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        if (sprite is not Damageable damageDealer) return;

        // Remove other Auras
        var hasBriarAura = damageDealer.Buffs.TryGetValue("Briarthorn Aura", out var briar);
        if (hasBriarAura)
            briar.OnEnded(damageDealer, briar);

        var buff = new aura_LawsOfAosda();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Damageable damageable) return;
        if (damageable.HasBuff("Laws of Aosda"))
        {
            OnFailed(sprite);
            return;
        }

        OnSuccess(sprite);
    }
}

[Script("Shield Bash")]
public class ShieldBash(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Shield Bash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Aisling damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(2);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }


            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);
                if (damageDealer.BlessedShield)
                    dmgCalc *= 2;
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            damageDealer.BlessedShield = false;
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not ("Shields"))
        {
            OnFailed(aisling);
            return;
        }

        var success = GlobalSkillMethods.OnUse(aisling, Skill);

        if (success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg = 0;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 40 + Skill.Level;
            dmg = client.Aisling.Str * 18 + client.Aisling.Dex * 10 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 2);
            dmg += dmg * imp / 100;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Blessed Shield")]
public class BlessedShield(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Must have a shield equipped to Bless");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageable) return;
        damageable.SendAnimationNearby(295, null, damageable.Serial);
        damageable.BlessedShield = true;
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not ("Shields"))
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Wrath Blow")]
public class WrathBlow(Skill skill) : SkillScript(skill)
{
    private Sprite _target;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Wrath Blow";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                _target = enemy;
                OnFailed(sprite);
                return;
            }

            var debuff = new DebuffWrathConsequences();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, _target, Skill);

            enemy.Target = damageDealer;
            var dmgCalc = DamageCalc(sprite);
            if (enemy is not Damageable damageable) return;
            GlobalSkillMethods.OnSuccess(damageable, damageDealer, Skill, dmgCalc, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    private long DamageCalc(Sprite sprite)
    {
        long dmg = 0;
        if (sprite is not Aisling damageDealingAisling) return dmg;
        var client = damageDealingAisling.Client;
        var imp = 50 + Skill.Level;
        dmg = client.Aisling.Str * 12 + client.Aisling.Int * 3;
        dmg += dmg * imp / 100;

        return dmg;
    }
}