using Chaos.Networking.Entities.Server;

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
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(9);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (sprite is Aisling aisling)
                {
                    aisling.ActionUsed = "Bang";
                    var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)thrown,
                        SourceId = aisling.Serial,
                        TargetAnimation = (ushort)thrown,
                        TargetId = i.Serial
                    };
                }
                else
                {
                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)(_crit ? 10002 : 10000),
                        SourceId = sprite.Serial,
                        TargetAnimation = (ushort)(_crit ? 10002 : 10000),
                        TargetId = i.Serial
                    };
                }

                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
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

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 16 + client.Aisling.Dex * 20 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 9);
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

// Aim after 5 seconds releases three deadly shots towards targets within your line of sight
[Script("Snipe")]
public class Snipe(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Snipe failed to set the target!");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(9);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            Task.Run(async () =>
            {
                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    if (!i.Alive) continue;
                    damageDealer.SendAnimationNearby(374, i.Position);
                }

                await Task.Delay(5000);

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    if (!i.Alive) continue;
                    var dmgCalc = DamageCalc(sprite, i);

                    if (sprite is Aisling aisling)
                    {
                        aisling.ActionUsed = "Snipe";
                        var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                        _animationArgs = new AnimationArgs
                        {
                            AnimationSpeed = 100,
                            SourceAnimation = (ushort)thrown,
                            SourceId = aisling.Serial,
                            TargetAnimation = (ushort)thrown,
                            TargetId = i.Serial
                        };
                    }
                    else
                    {
                        _animationArgs = new AnimationArgs
                        {
                            AnimationSpeed = 100,
                            SourceAnimation = (ushort)(_crit ? 10002 : 10000),
                            SourceId = sprite.Serial,
                            TargetAnimation = (ushort)(_crit ? 10002 : 10000),
                            TargetId = i.Serial
                        };
                    }

                    GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                    GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                    GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
                    damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
                }
            });

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

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
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Volley failed!");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(9).FirstOrDefault();
            List<Sprite> targets;
            Position tile;

            if (enemy is null)
            {
                tile = damageDealer.GetTilesInFront(9).LastOrDefault();
                targets = GetObjects(damageDealer.Map, i => i != null && i.WithinRangeOfTile(tile, 4), Get.Damageable).ToList();
            }
            else
            {
                targets = damageDealer.DamageableWithinRange(enemy, 4).ToList();
                targets.Add(enemy);
            }

            if (targets.IsEmpty())
            {
                OnFailed(sprite);
                return;
            }

            if (sprite is Aisling aisling)
            {
                aisling.ActionUsed = "Volley";
                var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                _animationArgs = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = (ushort)thrown,
                    TargetId = targets.FirstOrDefault()?.Serial
                };
            }
            else
            {
                _animationArgs = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)(_crit ? 10002 : 10000),
                    SourceId = sprite.Serial,
                    TargetAnimation = (ushort)(_crit ? 10002 : 10000),
                    TargetId = targets.FirstOrDefault()?.Serial
                };
            }

            foreach (var i in targets.Where(i => sprite.Serial != i.Serial))
            {
                if (!i.Alive) return;
                var dmgCalc = DamageCalc(sprite, i);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            if (targets.FirstOrDefault() != null)
                damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

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