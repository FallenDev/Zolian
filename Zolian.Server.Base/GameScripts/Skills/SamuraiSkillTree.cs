﻿using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Skills;

// Draw your sword, slashing forward and to the sides, dealing critical damage. Executes if less than 10% health
[Script("Iaido")]
public class Iaido(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private List<Sprite> _enemyList;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "My honour has not faltered..");
        GlobalSkillMethods.OnFailed(sprite, Skill, null);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Iaido";
        }

        if (_enemyList.Count == 0)
        {
            OnFailed(sprite);
            return;
        }

        try
        {
            var position = _enemyList.Last()?.Position;
            if (position == null)
            {
                OnFailed(sprite);
                return;
            }

            var mapCheck = sprite.Map.ID;
            if (sprite is not Damageable damageDealer) return;
            var wallPosition = damageDealer.GetPendingChargePositionNoTarget(6, damageDealer);
            var wallPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, wallPosition);

            if (mapCheck != damageDealer.Map.ID) return;
            if (!(wallPos > 0))
            {
                OnFailed(damageDealer);
                return;
            }

            if (damageDealer.Position != wallPosition)
            {
                GlobalSkillMethods.Step(damageDealer, wallPosition.X, wallPosition.Y);
            }

            foreach (var enemy in _enemyList)
            {
                if (enemy is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(damageDealer);

                GlobalSkillMethods.OnSuccessWithoutActionAnimation(enemy, damageDealer, Skill, dmgCalc, true);
                Task.Delay(300).ContinueWith(c =>
                {
                    damageDealer.SendAnimationNearby(119, enemy.Position);
                });

                if (!(enemy.CurrentHp <= enemy.MaximumHp * 0.10)) continue;
                switch (enemy)
                {
                    case Aisling:
                    case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                              || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                              || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                        if (damageDealer is Aisling player)
                            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to work on them");
                        continue;
                }
                var debuff = new DebuffReaping();
                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, enemy, Skill);
            }

            Task.Delay(300).ContinueWith(c =>
            {
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, damageDealer.Position);
            });

            if (wallPos > 5) return;

            var stunned = new DebuffBeagsuain();
            GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, stunned, damageDealer, Skill);
            damageDealer.SendAnimationNearby(208, damageDealer.Position);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _enemyList?.Clear();
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Damageable damageDealer) return;

        if (damageDealer.CantAttack)
        {
            OnFailed(sprite);
            return;
        }

        // Prevent Loss of Macro in Dojo areas
        if (damageDealer is Aisling aisling)
            if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
            {
                GlobalSkillMethods.Train(aisling.Client, Skill);
                OnFailed(aisling);
                return;
            }

        Target(damageDealer);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Str * 45 + client.Aisling.Dex * 43;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        // Always crit
        _crit = true;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Damageable damageDealer) return;

        try
        {
            _enemyList = damageDealer.DamageableGetInFrontColumn(6);

            if (_enemyList.Count == 0)
            {
                var mapCheck = damageDealer.Map.ID;
                var wallPosition = damageDealer.GetPendingChargePositionNoTarget(6, damageDealer);
                var wallPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, wallPosition);

                if (mapCheck != damageDealer.Map.ID) return;
                if (!(wallPos > 0)) OnFailed(damageDealer);

                if (damageDealer.Position != wallPosition)
                {
                    GlobalSkillMethods.Step(damageDealer, wallPosition.X, wallPosition.Y);
                }

                if (wallPos <= 5)
                {
                    var stunned = new DebuffBeagsuain();
                    GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, stunned, damageDealer, Skill);
                    damageDealer.SendAnimationNearby(208, damageDealer.Position);
                }

                if (damageDealer is Aisling aisling)
                    aisling.UsedSkill(Skill);
            }
            else
            {
                if (sprite is Aisling aisling)
                {
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
                else
                {
                    OnSuccess(sprite);
                }
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

// Slash that does massive mana damage, restoring your own
[Script("Mugai-ryu")]
public class MugaiRyu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Mugai-ryu";

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
            var enemy = damageDealer.GetAllInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);

                i.CurrentMp -= dmgCalc;
                if (i.CurrentMp <= 0)
                    i.CurrentMp = 0;

                damageDealer.CurrentMp += dmgCalc;
                if (damageDealer.CurrentMp > damageDealer.MaximumMp)
                    damageDealer.CurrentMp = damageDealer.MaximumMp;

                damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(i, 7));
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, 0, _crit);
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
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Int * 135 + client.Aisling.Wis * 150;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Slashes enemies eight times, dealing earth, wind, fire, water elemental damage
[Script("Niten Ichi Ryu")]
public class NitenIchiRyu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Niten Ichi Ryu";

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
            var enemy = damageDealer.GetAllInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 4);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Fire, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Earth, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Water, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Fire, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Earth, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Water, Skill);
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
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Str * 30 + client.Aisling.Con * 40;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Frontal Chain-Attack that attacks any enemy next to it, chaining outward
[Script("Shinto-ryu")]
public class ShintoRyu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Shinto-ryu";

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
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                OnFailed(sprite);
                return;
            }


            if (enemy is not Damageable damageable) return;
            var dmgCalc = DamageCalc(sprite);
            dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 5);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Void, Skill);
            GlobalSkillMethods.OnSuccessWithoutAction(enemy, damageDealer, Skill, dmgCalc, _crit);
            damageDealer.SendAnimationNearby(156, damageDealer.Position);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

            var chain = damageable.GetInFrontToSide();
            chain.AddRange(damageable.DamageableGetBehind());

            if (chain.Count == 0) return;

            foreach (var i in chain.Where(i => sprite.Serial != i.Serial))
            {
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Void, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            var chain2 = damageable.GetInFrontToSide();
            chain2.AddRange(damageable.DamageableGetBehind());

            if (chain2.Count == 0) return;

            foreach (var i in chain2.Where(i => sprite.Serial != i.Serial))
            {
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Void, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            var chain3 = damageable.GetInFrontToSide();
            chain3.AddRange(damageable.DamageableGetBehind());

            if (chain3.Count == 0) return;

            foreach (var i in chain3.Where(i => sprite.Serial != i.Serial))
            {
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Void, Skill);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }
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
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Int * 70 + client.Aisling.Str * 45;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// "One Sword" Expends 100% of your mana to deal a devastating frontal attack that adds the force of your chakra (mana) to it
[Script("Itto-ru")]
public class IttoRu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Itto-ru";

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
            var enemy = damageDealer.DamageableGetInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += damageDealer.CurrentMp;
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            damageDealer.CurrentMp = 0;
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

            if (damageDealer is Aisling vitaSend)
                vitaSend.Client.SendAttributes(StatUpdateType.FullVitality);
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
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Str * 70 + client.Aisling.Dex * 60;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Taunt that takes 100% threat of the enemy, deals a moderate amount of damage
[Script("Tamiya-ryu")]
public class TamiyaRyu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Tamiya-ryu";
            aisling.ThreatMeter += 100000000 * aisling.AbpLevel;
        }

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
            var enemy = damageDealer.GetHorizontalInFront();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                i.Target = damageDealer;
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
            var imp = (Skill.Level + 50 + damageDealingAisling.AbpLevel) * 2;
            dmg = client.Aisling.Str * 30 + client.Aisling.Dex * 54;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}