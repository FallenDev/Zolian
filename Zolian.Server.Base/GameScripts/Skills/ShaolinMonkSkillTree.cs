using System.Numerics;

using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Sprint towards nearest enemy dealing concentrated damage, otherwise rush forward 4 steps
[Script("Iron Sprint")]
public class IronSprint(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*listens to birds chirp nearby* I may have failed, but I won't falter");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iron Sprint";

        if (_target == null)
        {
            OnFailed(aisling);
            return;
        }

        try
        {
            var dmgCalc = DamageCalc(aisling);
            var targetPos = aisling.GetFromAllSidesEmpty(_target);
            if (targetPos == null || targetPos == _target.Position)
            {
                OnFailed(aisling);
                return;
            }

            GlobalSkillMethods.Step(aisling, targetPos.X, targetPos.Y);
            aisling.Facing(_target.X, _target.Y, out var direction);
            aisling.Direction = (byte)direction;
            aisling.Turn();
            if (_target is not Damageable damageable) return;
            damageable.ApplyDamage(aisling, dmgCalc, Skill);
            GlobalSkillMethods.Train(client, Skill);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));

            if (!_crit) return;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = Skill.Level * 3;
            dmg = client.Aisling.Str * 15 + client.Aisling.Dex * 15;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 15 + damageMonster.Dex * 15;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        try
        {
            _enemyList = aisling.DamageableWithinRange(aisling, 8);
            var closest = int.MaxValue;

            foreach (var enemy in _enemyList.Where(i => i.Serial != aisling.Serial && i is Monster))
            {
                var dist = aisling.DistanceFrom(enemy.X, enemy.Y);
                if (dist >= closest) continue;
                closest = dist;
                _target = enemy;
            }

            if (_target == null)
            {
                var mapCheck = aisling.Map.ID;
                var wallPosition = aisling.GetPendingChargePositionNoTarget(4, aisling);
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
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

// Punch with a concentrated blow using your stamina and strength, goes three tiles
[Script("Iron Fang")]
public class IronFang(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            aisling.ActionUsed = "Iron Fang";

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, i.Serial));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 30 + client.Aisling.Con * 20 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Claw fist, then a devastating holy blow
[Script("Golden Dragon Palm")]
public class GoldenDragonPalm(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.GetInFrontToSide();
            enemy.AddRange(aisling.GetInFrontToSide(2));

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            aisling.ActionUsed = "Golden Dragon Palm";
            GlobalSkillMethods.Train(aisling.Client, Skill);

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                if (_target is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Holy, Skill);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Holy, Skill);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, i.Serial));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);
            var buff = new buff_clawfist();
            GlobalSkillMethods.ApplyPhysicalBuff(aisling, buff);

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
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 35 + client.Aisling.Con * 30 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Kick twice dealing massive damage and throwing the enemy back 2 squares
[Script("Snake Whip")]
public class SnakeWhip(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Snake Whip";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.GetInFrontToSide();
            enemy.AddRange(aisling.GetInFrontToSide(2));

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in enemy.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, aisling, Skill, dmgCalc, _crit);
                ThrowBack(aisling, i);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Dex * 55 + client.Aisling.Con * 30;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 30;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private static void ThrowBack(Aisling aisling, Sprite target)
    {
        if (target is not Monster monster) return;
        try
        {
            if (monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)) return;
            var targetPosition = monster.GetPendingThrowPosition(2, monster);
            var hasHitOffWall = monster.GetPendingThrowIsWall(2, monster);
            var readyTime = DateTime.UtcNow;

            if (hasHitOffWall)
            {
                var stunned = new DebuffBeagsuain();
                stunned.OnApplied(monster, stunned);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(208, null, monster.Serial));
            }

            monster.Pos = new Vector2(targetPosition.X, targetPosition.Y);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendCreatureWalk(monster.Serial, new Point(targetPosition.X, targetPosition.Y),
                    (Direction)monster.Direction));
            monster.LastMovementChanged = readyTime;
            monster.LastPosition = new Position(targetPosition.X, targetPosition.Y);
            monster.ThrownBack = true;
            monster.UpdateAddAndRemove();
            Task.Delay(500).ContinueWith(ct => monster.ThrownBack = false);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with SnakeWhip ThrowBack called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with SnakeWhip ThrowBack called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

// Slash enemies in front and to the side of you four times with a powerful force using rage
[Script("Tiger Swipe")]
public class TigerSwipe(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Tiger Swipe";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.GetInFrontToSide();
            enemy.AddRange(aisling.GetInFrontToSide(2));
            enemy.AddRange(aisling.GetHorizontalInFront(2));

            var distinctEnemies = enemy.Distinct().ToList();

            if (distinctEnemies.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            GlobalSkillMethods.Train(aisling.Client, Skill);

            foreach (var i in distinctEnemies.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                if (_target is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Rage, Skill);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, i.Serial));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(73, aisling.Position));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 40 + client.Aisling.Dex * 40 * Math.Max(damageDealingAisling.Position.DistanceFrom(_target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(_target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Devastating punch that deals frontal and rear dmg, increases your damage by 200% for 10 seconds
[Script("Hardened Hands")]
public class HardenedHands(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Hardened Hands";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetBehind(2);
            enemy.AddRange(aisling.DamageableGetInFront(2));

            var distinctEnemies = enemy.Distinct().ToList();

            if (distinctEnemies.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                return;
            }

            foreach (var i in distinctEnemies.Where(i => aisling.Serial != i.Serial))
            {
                _target = i;
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, aisling, Skill, dmgCalc, _crit);
            }

            var buff = new BuffHardenedHands();
            GlobalSkillMethods.ApplyPhysicalBuff(aisling, buff);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.DamageableGetInFront().FirstOrDefault();
                _target = enemy;

                if (_target == null || _target.Serial == sprite.Serial)
                {
                    GlobalSkillMethods.FailedAttemptBodyAnimation(sprite);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 120 + client.Aisling.Con * 60;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 80 + damageMonster.Con * 60;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}