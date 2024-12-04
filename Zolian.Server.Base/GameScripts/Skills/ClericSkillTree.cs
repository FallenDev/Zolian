using Chaos.Networking.Entities.Server;

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

[Script("Blink")]
public class Blink(Skill skill) : SkillScript(skill)
{
    private Position _oldPosition;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "That didn't seem to work");
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
            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Skill.Template.Sound, false));
        }
        catch
        {
            // ignored
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Drag & Drop feather on map to use.");
    }

    public override void ItemOnDropped(Sprite sprite, Position pos, Area map)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Blink";
        if (map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This does not work here. What now?");
            return;
        }

        if (map.ID is >= 6530 and <= 6580)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "As you go to use your feather, something blocks it.");
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
                sprite.SendAnimationNearby(197, new Position(newPos.X, newPos.Y));
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
                sprite.SendAnimationNearby(197, new Position(newPos.X, newPos.Y));
            }
    }
}

[Script("Smite")]
public class Smite(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to purify");
        damageDealingAisling.SendAnimationNearby(76, null, damageDealingAisling.Serial);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Smite";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetHorizontalInFront();
            enemy.AddRange(damageDealer.GetInFrontToSide());

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is Monster)
                {
                    if (i.HasDebuff("Beag Suain"))
                        i.RemoveDebuff("Beag Suain");

                    var debuff = new DebuffFrozen();
                    GlobalSkillMethods.ApplyPhysicalDebuff(damageDealer, debuff, i, Skill);
                }

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
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Int * 2;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3 + damageMonster.Int * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Remedy")]
public class Remedy(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to focus");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Remedy";

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var manaSap = (long)(damageDealer.MaximumMp * .85);

            if (damageDealer.CurrentMp < manaSap)
            {
                OnFailed(damageDealer);
                return;
            }

            damageDealer.CurrentMp -= manaSap;

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.HandsUp,
                Sound = null,
                SourceId = sprite.Serial
            };

            foreach (var debuff in damageDealer.Debuffs.Values)
            {
                if (debuff.Name == "Skulled") continue;
                debuff.OnEnded(damageDealer, debuff);
            }

            GlobalSkillMethods.OnSuccess(damageDealer, damageDealer, Skill, 0, false, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        OnSuccess(sprite);
    }
}

[Script("Holy Lance")]
public class HolyLance(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Holy Lance";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(4);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite, i);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 3);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Holy, Skill);
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

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Int * 4 * Math.Max(damageDealingAisling.Position.DistanceFrom(target.Position), 4);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4 + damageMonster.Int * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Recite")]
public class Recite(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Recite Scripture";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.PriestCast,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 1);

                // Determine element
                var element = i.DefenseElement switch
                {
                    ElementManager.Element.None => ElementManager.Element.None,
                    ElementManager.Element.Fire => ElementManager.Element.Water,
                    ElementManager.Element.Wind => ElementManager.Element.Fire,
                    ElementManager.Element.Earth => ElementManager.Element.Wind,
                    ElementManager.Element.Water => ElementManager.Element.Earth,
                    ElementManager.Element.Holy => ElementManager.Element.Void,
                    ElementManager.Element.Void => ElementManager.Element.Holy,
                    ElementManager.Element.Rage => ElementManager.Element.Sorrow,
                    ElementManager.Element.Sorrow => ElementManager.Element.Terror,
                    ElementManager.Element.Terror => ElementManager.Element.Rage,
                    _ => ElementManager.Element.None
                };

                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, element, Skill);
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 7 + client.Aisling.Con * 7;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Con * 7 + damageMonster.Str * 7;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Unholy Swipe")]
public class UnHolySwipe(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "UnHoly Swipe";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 1);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Void, Skill);
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 10 + client.Aisling.Dex * 10;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 10 + damageMonster.Dex * 10;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Consecrated Strike")]
public class ConsecratedStrike(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Consecrated Strike";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.Assail,
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
                if (i is not Damageable damageable) continue;
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 1);
                damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Holy, Skill);
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 8 + client.Aisling.Con * 8;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 8 + damageMonster.Con * 8;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Divine Wrath")]
public class DivineWrath(Skill skill) : SkillScript(skill)
{
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Divine Wrath";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 20,
            BodyAnimation = BodyAnimation.PriestCast,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.MonstersNearby().ToList();

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }

            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite);
                dmgCalc += (int)GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, 1);
                var healthMultiplier = damageDealer.CurrentHp * 0.50;

                if (damageDealer.CurrentHp > healthMultiplier)
                {
                    damageDealer.CurrentHp = (int)healthMultiplier;
                    if (damageDealer is Aisling vitaAisling)
                        vitaAisling.Client.SendAttributes(StatUpdateType.Vitality);
                    dmgCalc += (long)healthMultiplier;
                }
                else
                {
                    OnFailed(damageDealer);
                    return;
                }

                i.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Holy, Skill);
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
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Int * 11 + client.Aisling.Con * 5;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 11 + damageMonster.Con * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}