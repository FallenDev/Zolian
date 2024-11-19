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

[Script("Wind Slice")]
public class Wind_Slice(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Wind Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(4);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront(4);

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 40,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = Skill.Reflect(i, sprite, Skill);
                    if (_target is not Damageable damageable) continue;
                    var dmgCalc = DamageCalc(sprite);

                    damageable.ApplyDamage(sprite, dmgCalc, Skill);

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Dual Slice")]
public class Dual_Slice(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Dual Slice";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = client.Aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront(3).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Blitz")]
public class Blitz(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Blitz";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var targetPos = aisling.GetFromAllSidesEmpty(_target);

            if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            GlobalSkillMethods.Step(aisling, targetPos.X, targetPos.Y);
            aisling.Facing(_target.X, _target.Y, out var direction);
            aisling.Direction = (byte)direction;
            aisling.Turn();
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
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
            dmg = (int)(client.Aisling.Str * 1 + client.Aisling.Dex * 1.3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 1 + damageMonster.Dex * 2;
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
            _enemyList = client.Aisling.DamageableGetInFront(3);
            _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                OnFailed(aisling);
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
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Aid")]
public class Aid(Skill skill) : SkillScript(skill)
{
    private Sprite _target;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Aid";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = client.Aisling.DamageableGetInFront();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy)
            {
                _target = Skill.Reflect(i, sprite, Skill);
                if (_target is not Damageable damageable) continue;

                switch (_target)
                {
                    case Aisling reviveAisling:
                        {
                            if (reviveAisling.Skulled)
                            {
                                reviveAisling.Debuffs.TryGetValue("Skulled", out var debuff);
                                if (debuff != null)
                                {
                                    debuff.Cancelled = true;
                                    debuff.OnEnded(reviveAisling, debuff);
                                    reviveAisling.Client.Revive();
                                }
                            }

                            if (reviveAisling.HasDebuff("Beag Suain"))
                                reviveAisling.Debuffs.TryRemove("Beag Suain", out _);

                            damageable.ApplyDamage(aisling, 0, Skill);
                            reviveAisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                                c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));
                            break;
                        }
                }
            }

            GlobalSkillMethods.Train(client, Skill);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        if (!Skill.CanUse()) return;
        aisling.Client.SendCooldown(true, Skill.Slot, Skill.Template.Cooldown);

        var success = Generator.RandNumGen100();

        if (success < 3)
        {
            OnFailed(aisling);
            return;
        }

        OnSuccess(aisling);
    }
}

[Script("Lullaby Strike")]
public class Lullaby_Strike(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to incapacitate!");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Lullaby Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            if (_target.HasDebuff("Beag Suain"))
                _target.RemoveDebuff("Beag Suain");

            var debuff = new DebuffFrozen();
            {
                if (_target.HasDebuff(debuff.Name))
                    _target.RemoveDebuff(debuff.Name);

                GlobalSkillMethods.ApplyPhysicalDebuff(aisling.Client, debuff, _target, Skill);
            }

            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, 0, false, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            var target = sprite.Target;

            switch (target)
            {
                case null:
                    return;
                case Aisling damageDealingTarget:
                    {
                        var debuff = new DebuffFrozen();
                        {
                            if (!damageDealingTarget.HasDebuff(debuff.Name))
                                GlobalSkillMethods.ApplyPhysicalDebuff(damageDealingTarget.Client, debuff, target, Skill);
                        }
                        break;
                    }
            }
        }
    }
}

[Script("Desolate")]
public class Desolate(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Desolate";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = client.Aisling.GetHorizontalInFront();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront(4);

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 40,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy == null) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = Skill.Reflect(i, sprite, Skill);
                    if (_target is not Damageable damageable) continue;
                    var dmgCalc = DamageCalc(sprite);

                    damageable.ApplyDamage(sprite, dmgCalc, Skill);

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 6;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Crasher")]
public class Crasher(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Crasher";

        var criticalHp = (long)(aisling.MaximumHp * .95);
        var crasherHp = (long)(aisling.CurrentHp * .05);

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.JumpAttack,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var dmg = (long)(criticalHp * 1.5);
            aisling.CurrentHp = crasherHp >= aisling.CurrentHp ? 1 : crasherHp;
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I feel drained...");
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmg, false, action);
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var dmg = (long)(sprite.CurrentHp * 1.5);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmg, false, action);
            }
            catch
            {
                // ignored
            }
        }
    }
}

[Script("Sever")]
public class Sever(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Sever";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Swipe,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront(4);

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 40,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = Skill.Reflect(i, sprite, Skill);
                    if (_target is not Damageable damageable) continue;
                    var dmgCalc = DamageCalc(sprite);

                    damageable.ApplyDamage(sprite, dmgCalc, Skill);

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = (int)(client.Aisling.Str * 5 + client.Aisling.Dex * 1.5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Rush")]
public class Rush(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rush";

        if (_target == null)
        {
            OnFailed(aisling);
            return;
        }

        try
        {
            var dmgCalc = DamageCalc(aisling);
            var position = _target.Position;
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(3, aisling);
            var targetPos = GlobalSkillMethods.DistanceTo(aisling.Position, position);
            var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (aisling.Direction)
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

                if (aisling.Position != position)
                {
                    GlobalSkillMethods.Step(aisling, position.X, position.Y);
                }

                if (_target is not Damageable damageable) return;
                damageable.ApplyDamage(aisling, dmgCalc, Skill);
                GlobalSkillMethods.Train(client, Skill);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));

                if (!_crit) return;
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(387, null, sprite.Serial));
            }
            else
            {
                GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);

                var stunned = new DebuffBeagsuain();
                aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(208, null, aisling.Serial));
            }
        }
        catch (Exception)
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
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
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
            _enemyList = client.Aisling.DamageableGetInFront(3);
            _target = _enemyList.FirstOrDefault();

            if (_target == null)
            {
                var mapCheck = aisling.Map.ID;
                var wallPosition = aisling.GetPendingChargePositionNoTarget(3, aisling);
                var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

                if (mapCheck != aisling.Map.ID) return;
                if (!(wallPos > 0)) OnFailed(aisling);

                if (aisling.Position != wallPosition)
                {
                    GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);
                }

                if (wallPos <= 2)
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
                OnSuccess(aisling);
            }
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

[Script("Titan's Cleave")]
public class Titans_Cleave(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Titan's Cleave";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(aisling);
                var debuff = new DebuffTitansCleave();

                if (!_target.HasDebuff(debuff.Name))
                    aisling.Client.EnqueueDebuffAppliedEvent(_target, debuff);
                if (_target is Aisling targetPlayer)
                    targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var debuff = new DebuffTitansCleave();

                if (_target is Aisling targetPlayer)
                {
                    if (!_target.HasDebuff(debuff.Name))
                    {
                        targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff);
                        targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);
                    }
                }
                else
                {
                    if (!_target.HasDebuff(debuff.Name))
                        debuff.OnApplied(_target, debuff);
                }

                var dmg = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmg, false, action);
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 6 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += (int)(damageMonster.Con * 1.2);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Retribution")]
public class Retribution(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Retribution";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.TwoHandAtk,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.GetInFrontToSide();
            var enemyTwo = aisling.DamageableGetBehind();
            enemy.Add(enemyTwo.FirstOrDefault());

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => i != null && aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                var debuff = new DebuffRetribution();

                if (!_target.HasDebuff(debuff.Name))
                    aisling.Client.EnqueueDebuffAppliedEvent(_target, debuff);
                if (_target is Aisling targetPlayer)
                    targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);

                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
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
        else
        {
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 40,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var debuff = new DebuffRetribution();

                if (_target is Aisling targetPlayer)
                {
                    if (!_target.HasDebuff(debuff.Name))
                    {
                        targetPlayer.Client.EnqueueDebuffAppliedEvent(_target, debuff);
                        targetPlayer.Client.SendAttributes(StatUpdateType.Secondary);
                    }
                }
                else
                {
                    if (!_target.HasDebuff(debuff.Name))
                        debuff.OnApplied(_target, debuff);
                }

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 9 + client.Aisling.Dex * 3 + client.Aisling.Con * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 9;
            dmg += damageMonster.Con * 3 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Sneak Attack")]
public class Sneak_Attack(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Sneak Attack";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = BodyAnimation.Jump,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var targetPos = aisling.GetFromAllSidesEmpty(_target);

            if (_target == null || _target.Serial == aisling.Serial || targetPos == _target.Position)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            GlobalSkillMethods.Step(aisling, targetPos.X, targetPos.Y);
            aisling.Facing(_target.X, _target.Y, out var direction);
            aisling.Direction = (byte)direction;
            aisling.Turn();
            GlobalSkillMethods.OnSuccess(_target, aisling, Skill, dmgCalc, _crit, action);
        }
        catch (Exception)
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
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        _enemyList = client.Aisling.DamageableGetInFront();
        _target = _enemyList.FirstOrDefault();

        if (_target == null)
        {
            OnFailed(aisling);
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
}

[Script("Berserk")]
public class Berserk(Skill skill) : SkillScript(skill)
{
    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to focus");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Berserk";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var buff = new buff_berserk();
        {
            GlobalSkillMethods.ApplyPhysicalBuff(aisling, buff);
        }

        GlobalSkillMethods.OnSuccess(aisling, aisling, Skill, 0, false, action);
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;
        OnSuccess(aisling);
    }
}