using Chaos.Networking.Entities.Server;

using Darkages.Enums;
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
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite is not Damageable damageable) return;
        if (damageable.NextTo(_target.Position.X, _target.Position.Y) &&
            damageable.Facing(_target.Position.X, _target.Position.Y, out _))
            damageable.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        var enemy = aisling.DamageableGetInFront(9);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        aisling.ActionUsed = "Bang";

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var thrown = _skillMethod.Thrown(aisling.Client, Skill, _crit);

            var animation = new AnimationArgs
            {
                AnimationSpeed = 100,
                SourceAnimation = (ushort)thrown,
                SourceId = aisling.Serial,
                TargetAnimation = (ushort)thrown,
                TargetId = i.Serial
            };

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
            aisling.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
        }

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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

            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.MonsterGetInFront(5).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, Skill, action);
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

            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Aim after 5 seconds releases three deadly shots towards targets within your line of sight
[Script("Snipe")]
public class Snipe(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Snipe failed to set the target!");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        var enemy = aisling.DamageableGetInFront(9);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        aisling.ActionUsed = "Snipe";

        Task.Run(async () =>
        {
            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
            {
                _target = i;
                if (!_target.Alive) return;
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(374, _target.Position));
            }

            await Task.Delay(5000);

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
            {
                _target = i;
                if (!_target.Alive) return;

                var thrown = _skillMethod.Thrown(aisling.Client, Skill, _crit);

                var animation = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = (ushort)thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = (ushort)thrown,
                    TargetId = i.Serial
                };

                var dmgCalc = DamageCalc(sprite);
                _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                _skillMethod.OnSuccessWithoutAction(_target, aisling, Skill, dmgCalc, _crit);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            }
        });

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, Skill);

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

            if (sprite is not Identifiable identifiable) return;
            var enemy = identifiable.MonsterGetInFront(5).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, Skill, action);
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

            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
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
            dmg = client.Aisling.Str * 25 + client.Aisling.Dex * 20 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 9);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 12 + damageMonster.Dex * 15 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 9);
        }

        var critCheck = _skillMethod.OnCrit(dmg);
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
    private readonly GlobalSkillMethods _skillMethod = new();

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

        var enemy = aisling.DamageableGetInFront(9).FirstOrDefault();
        List<Sprite> targets;
        Position tile;

        if (enemy is null)
        {
            tile = aisling.GetTilesInFront(9).LastOrDefault();
            targets = GetObjects(aisling.Map, i => i != null && i.WithinRangeOfTile(tile, 4), Get.AislingDamage).ToList();
        }
        else
        {
            targets = GetObjects(aisling.Map, i => i != null && i.WithinRangeOf(enemy, 4), Get.AislingDamage).ToList();
            targets.Add(enemy);
        }

        if (targets.IsEmpty())
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in targets.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            if (!i.Alive) return;
            var dmgCalc = DamageCalc(sprite, i);
            _skillMethod.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
        }

        var thrown = _skillMethod.Thrown(aisling.Client, Skill, _crit);

        var animation = new AnimationArgs
        {
            AnimationSpeed = 100,
            SourceAnimation = (ushort)thrown,
            SourceId = aisling.Serial,
            TargetAnimation = (ushort)thrown,
            TargetId = targets.First().Serial
        };

        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, Skill);

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

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}