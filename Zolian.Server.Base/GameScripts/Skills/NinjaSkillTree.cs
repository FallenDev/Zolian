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

// Amenotejikara for teleporting to a location on the map
[Script("Amenotejikara")]
public class Amenotejikara(Skill skill) : SkillScript(skill)
{
    private Position _oldPosition;

    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I can't move there");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            GlobalSkillMethods.Train(aisling.Client, Skill);

        try
        {
            if (sprite is not Damageable damageDealer) return;
            SendPortAnimation(damageDealer, _oldPosition);
            damageDealer.SendAnimationNearby(76, null, damageDealer.Serial);
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSoundImmediate(Skill.Template.Sound, false));
        }
        catch
        {
            // ignored
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Drag & Drop Chakra Stone on map to use.");
    }

    public override void ItemOnDropped(Sprite sprite, Position pos, Area map)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Amenotejikara";
        if (map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This does not work here. What now?");
            return;
        }

        if (map.ID is >= 6530 and <= 6580)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "As you go to use your ability, something blocks it.");
            return;
        }

        try
        {
            _oldPosition = sprite.Position;
            damageDealingSprite.FacingFarAway(pos.X, pos.Y, out var direction);
            damageDealingSprite.Direction = (byte)direction;
            client.WarpToAndRefresh(pos);
            OnSuccess(damageDealingSprite);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within ItemOnDropped called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within ItemOnDropped called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private static void SendPortAnimation(Damageable sprite, Position pos)
    {
        var orgPos = sprite.Pos;
        var xDiff = orgPos.X - pos.X;
        var yDiff = orgPos.Y - pos.Y;
        var xGap = Math.Abs(xDiff);
        var yGap = Math.Abs(yDiff);
        var xDiffHold = 0;
        var yDiffHold = 0;

        if (yGap > xGap)
            for (var i = 0; i < yGap; i++)
            {
                switch (yDiff)
                {
                    case < 0:
                        yDiffHold++;
                        break;
                    case > 0:
                        yDiffHold--;
                        break;
                }

                var newPos = orgPos with { Y = orgPos.Y + yDiffHold };
                sprite.SendAnimationNearby(55, new Position(newPos.X, newPos.Y));
            }
        else
            for (var i = 0; i < xGap; i++)
            {
                switch (xDiff)
                {
                    case < 0:
                        xDiffHold++;
                        break;
                    case > 0:
                        xDiffHold--;
                        break;
                }

                var newPos = orgPos with { X = orgPos.X + xDiffHold };
                sprite.SendAnimationNearby(55, new Position(newPos.X, newPos.Y));
            }
    }
}

// Chakra Blast for massive damage in front of you up to 3 tiles
[Script("Rasengan")]
public class Rasengan(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Rasengan";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Stab,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(3);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (damageDealer.HasBuff("Rasen Shoheki"))
                    dmgCalc *= 2; // Rasen Shoheki doubles damage dealt

                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, damageDealer, Skill, dmgCalc * 2, _crit);
                GlobalSkillMethods.OnSuccess(i, damageDealer, Skill, dmgCalc * 3, _crit, action);
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

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Con * 75 + client.Aisling.Dex * 80 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 3);
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

// Chakra Barrier blocking damage for up to 30 seconds, doubles ninja skills damage
[Script("Rasen Shoheki")]
public class RasenShoheki(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Rasen Shoheki";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var buff = new buff_RasenShoheki();
            aisling.Client.EnqueueBuffAppliedEvent(aisling, buff);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}

// Shadow Shuriken deals high damage and hits twice
[Script("Shadow Shuriken")]
public class ShadowShuriken(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Shadow Shuriken";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);
                if (aisling.HasBuff("Rasen Shoheki"))
                    dmgCalc *= 2; // Rasen Shoheki doubles damage dealt

                aisling.ActionUsed = "Shadow Shuriken";
                const int thrown = 10011; // Animation for throwing the shuriken

                _animationArgs = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = thrown,
                    TargetId = i.Serial
                };

                GlobalSkillMethods.OnSuccessWithoutActionAnimation(i, sprite, Skill, dmgCalc, _crit);
                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                aisling.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 75 + client.Aisling.Dex * 140 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Dex * 3 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Flames that are "legend" to never go out until the target is dead - (Flame DoT)
[Script("Amaterasu")]
public class Amaterasu(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealer) return;
        damageDealer.ActionUsed = "Amaterasu";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (damageDealer.HasBuff("Rasen Shoheki"))
                    dmgCalc *= 2; // Rasen Shoheki doubles damage dealt

                GlobalSkillMethods.OnSuccess(i, damageDealer, Skill, dmgCalc, _crit, action);

                // Apply the Amaterasu debuff to the target
                var debuff = new DebuffAmaterasu();
                if (!i.HasDebuff(debuff.Name)) damageDealer.Client.EnqueueDebuffAppliedEvent(i, debuff);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level + damageDealingAisling.ExpLevel + damageDealingAisling.AbpLevel;
            dmg = client.Aisling.Int * 160 + client.Aisling.Dex * 80 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 3);
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

// Kunai for throwing a shuriken at the target, massive assail damage
[Script("Kunai")]
public class Kunai(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Kunai";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            var enemy = aisling.DamageableGetInFront(5);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);
                if (aisling.HasBuff("Rasen Shoheki"))
                    dmgCalc *= 2; // Rasen Shoheki doubles damage dealt

                aisling.ActionUsed = "Kunai";
                const int thrown = 10011; // Animation for throwing the shuriken

                _animationArgs = new AnimationArgs
                {
                    AnimationSpeed = 100,
                    SourceAnimation = thrown,
                    SourceId = aisling.Serial,
                    TargetAnimation = thrown,
                    TargetId = i.Serial
                };

                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                aisling.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 30 + client.Aisling.Dex * 70 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 5);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Dex * 3 * Math.Max(damageMonster.Position.DistanceFrom(target.Position), 3);
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Blend in with the environment, becoming invisible for eight hours
[Script("Blend")]
public class Blend(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to blend in.");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Damageable damageDealer) return;

        if (!sprite.Alive || sprite.IsInvisible)
        {
            OnFailed(sprite, null);
            return;
        }

        var buff = new buff_advHide();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        damageDealer.SendAnimationNearby(319, null, damageDealer.Serial);

        if (damageDealer is Aisling aisling)
            GlobalSkillMethods.Train(aisling.Client, Skill);
    }
}