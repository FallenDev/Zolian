﻿using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using ServiceStack;

namespace Darkages.GameScripts.Skills;

// Extremely high ranged dps
[Script("Bang")]
public class Bang(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Bang";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(9);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = (ushort)thrown,
                    TargetId = i.Serial
                };

                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.DamageableGetInFront(5).FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    SourceId = sprite.Serial,
                    TargetAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    TargetId = _target.Serial
                };

                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 16 + client.Aisling.Dex * 20 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 9);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 12 + damageMonster.Dex * 15 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 9);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aim after 5 seconds releases three deadly shots towards targets within your line of sight
[Script("Snipe")]
public class Snipe(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Snipe failed to set the target!");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Snipe";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(9);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            Task.Run(async () =>
            {
                foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
                {
                    if (!i.Alive) return;
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(374, i.Position));
                }

                await Task.Delay(5000);

                foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
                {
                    if (!i.Alive) return;

                    var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                    var animation = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)thrown,
                        SourceId = aisling.Serial,
                        TargetAnimation = (ushort)thrown,
                        TargetId = i.Serial
                    };

                    var dmgCalc = DamageCalc(sprite, i);
                    GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                    GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                    GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                            animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
                }
            });

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

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
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.DamageableGetInFront(5).FirstOrDefault();

                if (enemy == null || enemy.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite, action);
                    OnFailed(sprite);
                    return;
                }

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    SourceId = sprite.Serial,
                    TargetAnimation = (ushort)(_crit
                        ? 10002
                        : 10000),
                    TargetId = enemy.Serial
                };

                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0,
                        animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
                var dmgCalc = DamageCalc(sprite, enemy);
                GlobalSkillMethods.OnSuccess(enemy, sprite, Skill, dmgCalc, _crit, action);
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 25 + client.Aisling.Dex * 20 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 9);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 12 + damageMonster.Dex * 15 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 9);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Shoot a volley at an initial target's location x6 grid
[Script("Volley")]
public class Volley(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Volley failed!");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Volley";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(9).FirstOrDefault();
            List<Sprite> targets;
            Position tile;

            if (enemy is null)
            {
                tile = aisling.GetTilesInFront(9).LastOrDefault();
                targets = GetObjects(aisling.Map, i => i != null && i.WithinRangeOfTile(tile, 4), Get.Damageable)
                    .ToList();
            }
            else
            {
                targets = aisling.DamageableWithinRange(enemy, 4).ToList();
                targets.Add(enemy);
            }

            if (targets.IsEmpty())
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            foreach (var i in targets.Where(i => aisling.Serial != i.Serial))
            {
                if (!i.Alive) return;
                var dmgCalc = DamageCalc(sprite, i);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
            }

            var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

            var animation = new AnimationArgs
            {
                AnimationSpeed = 100,
                SourceAnimation = (ushort)thrown,
                SourceId = aisling.Serial,
                TargetAnimation = (ushort)thrown,
                TargetId = targets.FirstOrDefault()?.Serial
            };

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed,
                    animation.SourceAnimation, animation.SourceId ?? 0));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch (Exception)
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

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 10 + client.Aisling.Dex * 14 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 9);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 12 + damageMonster.Dex * 15 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 9);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}