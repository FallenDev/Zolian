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
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, "My honour has not faltered..");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iaido";

        if (_enemyList.Count == 0)
        {
            OnFailed(aisling);
            return;
        }

        try
        {
            var position = _enemyList.Last()?.Position;
            if (position == null)
            {
                OnFailed(aisling);
                return;
            }

            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePositionNoTarget(6, aisling);
            var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;
            if (!(wallPos > 0)) OnFailed(aisling);

            if (aisling.Position != wallPosition)
            {
                GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);
            }

            foreach (var enemy in _enemyList)
            {
                if (enemy is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(aisling);

                damageable.ApplyDamage(aisling, dmgCalc, Skill);
                GlobalSkillMethods.Train(client, Skill);
                Task.Delay(300).ContinueWith(c =>
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(119, enemy.Position));
                });

                if (!_crit) continue;
                Task.Delay(300).ContinueWith(c =>
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                });
            }

            Task.Delay(300).ContinueWith(c =>
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, aisling.Position));
            });

            if (wallPos > 5) return;

            var stunned = new DebuffBeagsuain();
            aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendAnimation(208, null, aisling.Serial));
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
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
        {
            OnFailed(aisling);
            return;
        }

        // Prevent Loss of Macro in Dojo areas
        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
        {
            GlobalSkillMethods.Train(client, Skill);
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
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        try
        {
            _enemyList = client.Aisling.DamageableGetInFrontColumn(6);

            if (_enemyList.Count == 0)
            {
                var mapCheck = aisling.Map.ID;
                var wallPosition = aisling.GetPendingChargePositionNoTarget(6, aisling);
                var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

                if (mapCheck != aisling.Map.ID) return;
                if (!(wallPos > 0)) OnFailed(aisling);

                if (aisling.Position != wallPosition)
                {
                    GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);
                }

                if (wallPos <= 5)
                {
                    var stunned = new DebuffBeagsuain();
                    aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(208, null, aisling.Serial));
                }

                aisling.UsedSkill(Skill);
            }
            else
            {
                _success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (_success)
                {
                    OnSuccess(aisling);
                }
                else
                {
                    OnFailed(aisling);
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

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Mugai-ryu";

    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;


    }
}

// Slashes enemies eight times, dealing earth, wind, fire, water elemental damage
[Script("Niten Ichi Ryu")]
public class NitenIchiRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
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

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
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

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
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

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
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