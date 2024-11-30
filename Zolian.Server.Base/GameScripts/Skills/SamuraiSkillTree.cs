using Darkages.Enums;
using Darkages.GameScripts.Affects;
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
            GlobalSkillMethods.Train(aisling.Client, Skill);
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
                var dmgCalc = DamageCalc(damageable);

                damageable.ApplyDamage(damageDealer, dmgCalc, Skill);
                Task.Delay(300).ContinueWith(c =>
                {
                    damageDealer.SendAnimationNearby(119, enemy.Position);
                });

                if (!_crit) continue;
                Task.Delay(300).ContinueWith(c =>
                {
                    damageDealer.SendAnimationNearby(387, null, damageDealer.Serial);
                });
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
        if (damageDealer is Aisling aisling && aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
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
        _crit = critCheck.Item1;
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Mugai-ryu";

    }

    public override void OnCleanup() { }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 5;
            dmg += client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 3;
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
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Niten Ichi Ryu";

    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

    }
}

// Frontal Chain-Attack that attacks any enemy next to it, chaining outward
[Script("Shinto-ryu")]
public class ShintoRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iaido";
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {

    }
}

// "One Sword" Expends 100% of your mana to deal a devastating frontal attack that adds the force of your chakra (mana) to it
[Script("Itto-ru")]
public class IttoRu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Itto-ru";
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {

    }
}

// Taunt that takes 100% threat of the enemy, deals a moderate amount of damage
[Script("Tamiya-ryu")]
public class TamiyaRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Tamiya-ryu";
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

    }
}