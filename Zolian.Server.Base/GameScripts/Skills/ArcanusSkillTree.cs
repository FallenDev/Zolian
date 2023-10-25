using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;
using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Flame Thrower")]
public class Flame_Thrower(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) &&
            sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

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

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        // enemy.Count = 0 verified that there is an enemy
        _target = enemy.First();

        if (_target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.SpellReflect)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, aisling);
        }

        if (_target.SpellNegate)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, skill);
        _skillMethod.OnSuccess(_target, aisling, skill, 0, false, action);
    }

    public override async void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, skill);

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
            var enemy = sprite.MonsterGetInFront(6);

            if (enemy.Count == 0) return;
            await SendAnimations(sprite, enemy);

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        foreach (var position in damageDealingSprite.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.First().Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageDealingSprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(147, vector));
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
            var imp = 10 + skill.Level;
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

        var critCheck = _skillMethod.OnCrit(dmg);
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
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) &&
            sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

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

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        // enemy.Count = 0 verified that there is an enemy
        _target = enemy.First();

        if (_target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.SpellReflect)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, aisling);
        }

        if (_target.SpellNegate)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Water, skill);
        _skillMethod.OnSuccess(_target, aisling, skill, 0, false, action);
    }

    public override async void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, skill);

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
            var enemy = sprite.MonsterGetInFront(6);

            if (enemy.Count == 0) return;
            await SendAnimations(sprite, enemy);

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Water, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        foreach (var position in damageDealingSprite.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.First().Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageDealingSprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(150, vector));
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
            var imp = 10 + skill.Level;
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

        var critCheck = _skillMethod.OnCrit(dmg);
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
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) &&
            sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

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

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        // enemy.Count = 0 verified that there is an enemy
        _target = enemy.First();

        if (_target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.SpellReflect)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, aisling);
        }

        if (_target.SpellNegate)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, skill);
        _skillMethod.OnSuccess(_target, aisling, skill, 0, false, action);
    }

    public override async void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, skill);

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
            var enemy = sprite.MonsterGetInFront(6);

            if (enemy.Count == 0) return;
            await SendAnimations(sprite, enemy);

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        foreach (var position in damageDealingSprite.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.First().Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageDealingSprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, vector));
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
            var imp = 10 + skill.Level;
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

        var critCheck = _skillMethod.OnCrit(dmg);
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
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) &&
            sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

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

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        // enemy.Count = 0 verified that there is an enemy
        _target = enemy.First();

        if (_target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.SpellReflect)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, aisling);
        }

        if (_target.SpellNegate)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, skill);
        _skillMethod.OnSuccess(_target, aisling, skill, 0, false, action);
    }

    public override async void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, skill);

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
            var enemy = sprite.MonsterGetInFront(6);

            if (enemy.Count == 0) return;
            await SendAnimations(sprite, enemy);

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
            }
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        foreach (var position in damageDealingSprite.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.First().Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageDealingSprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(60, vector));
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
            var imp = 10 + skill.Level;
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

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}