using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Flame Thrower")]
public class Flame_Thrower(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Flame Thrower";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            if (aisling.CurrentMp - 300 > 0)
            {
                aisling.CurrentMp -= 300;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var enemy = client.Aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            await SendAnimations(aisling, enemy);

            // enemy.Count = 0 verified that there is an enemy
            _target = enemy.FirstOrDefault();

            if (_target is null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            if (_target is not Damageable damageable) return;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                    "Your elemental ability has been reflected!");
                _target = Spell.SpellReflect(_target, aisling);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(64, null, _target.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, Skill);
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

    public override async void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Wands" ||
                client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Staves")
            {
                _success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (_success)
                {
                    OnSuccess(aisling);
                    return;
                }
            }

            OnFailed(aisling);
        }
        else
        {
            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(6);

                if (enemy.Count == 0) return;
                await SendAnimations(sprite, enemy);

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) return;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
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

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(147, vector));
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(_target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Water Cannon")]
public class Water_Cannon(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Water Cannon";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            if (aisling.CurrentMp - 300 > 0)
            {
                aisling.CurrentMp -= 300;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var enemy = client.Aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            await SendAnimations(aisling, enemy);

            // enemy.Count = 0 verified that there is an enemy
            _target = enemy.FirstOrDefault();

            if (_target is null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            if (_target is not Damageable damageable) return;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                    "Your elemental ability has been reflected!");
                _target = Spell.SpellReflect(_target, aisling);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(64, null, _target.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Water, Skill);
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

    public override async void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Wands" ||
                client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Staves")
            {
                _success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (_success)
                {
                    OnSuccess(aisling);
                    return;
                }
            }

            OnFailed(aisling);
        }
        else
        {
            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(6);

                if (enemy.Count == 0) return;
                await SendAnimations(sprite, enemy);

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) return;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Water, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
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

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(150, vector));
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(_target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tornado Vector")]
public class Tornado_Vector(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Tornado Vector";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            if (aisling.CurrentMp - 300 > 0)
            {
                aisling.CurrentMp -= 300;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var enemy = client.Aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            await SendAnimations(aisling, enemy);

            // enemy.Count = 0 verified that there is an enemy
            _target = enemy.FirstOrDefault();

            if (_target is null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            if (_target is not Damageable damageable) return;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                    "Your elemental ability has been reflected!");
                _target = Spell.SpellReflect(_target, aisling);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(64, null, _target.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, Skill);
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

    public override async void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Wands" ||
                client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Staves")
            {
                _success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (_success)
                {
                    OnSuccess(aisling);
                    return;
                }
            }

            OnFailed(aisling);
        }
        else
        {
            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(6);

                if (enemy.Count == 0) return;
                await SendAnimations(sprite, enemy);

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) return;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
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

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, vector));
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(_target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Earth Shatter")]
public class Earth_Shatter(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    public override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Earth Shatter";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = aisling.Serial
        };

        try
        {
            if (aisling.CurrentMp - 300 > 0)
            {
                aisling.CurrentMp -= 300;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            var enemy = client.Aisling.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            await SendAnimations(aisling, enemy);

            // enemy.Count = 0 verified that there is an enemy
            _target = enemy.FirstOrDefault();

            if (_target is null || _target.Serial == aisling.Serial)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling, action);
                OnFailed(aisling);
                return;
            }

            if (_target is not Damageable damageable) return;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                    "Your elemental ability has been reflected!");
                _target = Spell.SpellReflect(_target, aisling);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(64, null, _target.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(aisling);
            damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, Skill);
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

    public override async void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Wands" ||
                client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is "Staves")
            {
                _success = GlobalSkillMethods.OnUse(aisling, Skill);

                if (_success)
                {
                    OnSuccess(aisling);
                    return;
                }
            }

            OnFailed(aisling);
        }
        else
        {
            try
            {
                if (sprite is not Damageable identified) return;
                var enemy = identified.DamageableGetInFront(6);

                if (enemy.Count == 0) return;
                await SendAnimations(sprite, enemy);

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) return;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
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

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(60, vector));
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(_target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}