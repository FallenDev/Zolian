using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Grief Eruption")]
public class Grief_Eruption(Skill skill) : SkillScript(skill)
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
            sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.MissAnimation, null, _target.Serial));
    }

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Grief Eruption";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = aisling.Serial
        };

        if (aisling.CurrentMp - 1500 > 0)
        {
            aisling.CurrentMp -= 1500;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        // enemy.Count = 0 verified that there is an enemy
        _target = enemy.First();

        if (_target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, Skill, action);
            OnFailed(aisling);
            return;
        }

        if (_target.SpellReflect)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, aisling);
        }

        if (_target.SpellNegate)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(aisling);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Sorrow, Skill);
        _skillMethod.OnSuccess(_target, aisling, Skill, 0, false, action);
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

            OnFailed(aisling);
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
                    _target.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling)
                        _target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, Skill);

                if (Skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(sprite.Serial, BodyAnimation.Assail, 30));
                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
                damageDealingSprite.PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(120, vector));
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
            dmg = client.Aisling.Int * 35 + client.Aisling.Wis * 25 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 12;
            dmg += damageMonster.Wis * 10;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}