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
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Skills;

// Sprint towards nearest enemy dealing concentrated damage, otherwise rush forward 4 steps
[Script("Iron Sprint")]
public class IronSprint(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*listens to birds chirp nearby* I may have failed, but I won't falter");
        GlobalSkillMethods.OnFailed(sprite, Skill, null);
    }

    protected override async void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Iron Sprint";
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
            var targetPos = damageDealer.GetFromAllSidesEmpty(_target);
            if (targetPos == null || targetPos == _target.Position)
            {
                OnFailed(damageDealer);
                return;
            }

            var stepped = await damageDealer.StepAndRemove(damageDealer, targetPos.X, targetPos.Y);

            if (!stepped)
            {
                OnFailed(sprite);
                return;
            }

            damageDealer.Facing(_target.X, _target.Y, out var direction);
            damageDealer.Direction = (byte)direction;
            damageDealer.StepAddAndUpdateDisplay(damageDealer);
            if (_target is not Damageable damageable) return;
            var dmgCalc = DamageCalc(damageDealer);

            damageable.ApplyDamage(damageDealer, dmgCalc, Skill);
            damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, _target.Serial);

            if (!_crit) return;
            damageDealer.SendAnimationNearby(387, null, sprite.Serial);
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
            var imp = Skill.Level * 3;
            dmg = client.Aisling.Str * 75 + client.Aisling.Dex * 55;
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

    private async void Target(Sprite sprite)
    {
        if (sprite is not Damageable damageDealer) return;

        try
        {
            _enemyList = damageDealer.DamageableWithinRange(damageDealer, 8);
            var closest = int.MaxValue;

            foreach (var enemy in _enemyList.Where(i => i.Serial != sprite.Serial && i is Monster))
            {
                var dist = damageDealer.DistanceFrom(enemy.X, enemy.Y);
                if (dist >= closest) continue;
                closest = dist;
                _target = enemy;
            }

            if (_target == null)
            {
                var mapCheck = damageDealer.Map.ID;
                var wallPosition = damageDealer.GetPendingChargePositionNoTarget(4, damageDealer);
                var wallPos = GlobalSkillMethods.DistanceTo(damageDealer.Position, wallPosition);

                if (mapCheck != damageDealer.Map.ID) return;
                if (!(wallPos > 0))
                {
                    OnFailed(damageDealer);
                    return;
                }

                if (damageDealer.Position != wallPosition)
                {
                    var stepped = await damageDealer.StepAndRemove(damageDealer, wallPosition.X, wallPosition.Y);

                    if (!stepped)
                    {
                        OnFailed(sprite);
                        return;
                    }

                    damageDealer.StepAddAndUpdateDisplay(damageDealer);
                }

                if (wallPos <= 2)
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
                OnSuccess(damageDealer);
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
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Iron Fang";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
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
                var dmgCalc = DamageCalc(sprite, i);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc * 2, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc * 3, _crit);
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, i.Serial);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 60 + client.Aisling.Con * 80 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
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
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Golden Dragon Palm";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var buff = new buff_clawfist();
            GlobalSkillMethods.ApplyPhysicalBuff(sprite, buff);

            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            enemy.AddRange(damageDealer.GetInFrontToSide(2));

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite, i);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Holy, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc * 2, ElementManager.Element.Holy, Skill);
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, i.Serial);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 65 + client.Aisling.Con * 70 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
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
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Snake Whip";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.RoundHouseKick,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            enemy.AddRange(damageDealer.GetInFrontToSide(2));

            if (enemy.Count == 0)
            {
                OnFailed(damageDealer);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc * 2, _crit);
                ThrowBack(damageDealer, i);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Dex * 55 + client.Aisling.Con * 65;
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

    private static void ThrowBack(Damageable damageDealer, Sprite target)
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
                damageDealer.SendAnimationNearby(208, null, monster.Serial);
            }

            monster.Pos = new Vector2(targetPosition.X, targetPosition.Y);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendCreatureWalk(monster.Serial, new Point(targetPosition.X, targetPosition.Y), (Direction)monster.Direction));
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
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Tiger Swipe";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();
            enemy.AddRange(damageDealer.GetInFrontToSide(2));
            enemy.AddRange(damageDealer.GetHorizontalInFront(2));

            var distinctEnemies = enemy.Distinct().ToList();

            if (distinctEnemies.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in distinctEnemies.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite, i);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Rage, Skill);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Rage, Skill);
                damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, i.Serial);
            }

            damageDealer.SendAnimationNearby(73, damageDealer.Position);
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
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Str * 65 + client.Aisling.Dex * 65 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 3);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 30 + damageMonster.Con * 20 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
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
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Hardened Hands";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Punch,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetBehind(2);
            enemy.AddRange(damageDealer.DamageableGetInFront(2));

            var distinctEnemies = enemy.Distinct().ToList();

            if (distinctEnemies.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in distinctEnemies.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                GlobalSkillMethods.OnSuccessWithoutAction(i, damageDealer, Skill, dmgCalc, _crit);
            }

            var buff = new BuffHardenedHands();
            GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
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