using System.Numerics;

using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Flame Thrower")]
public class Flame_Thrower : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Flame_Thrower(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Flame Thrower";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x06,
            Speed = 40
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);
        _target = enemy.FirstOrDefault();

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        await SendAnimations(aisling, enemy);

        if (_target.SpellReflect)
        {
            _target.Animate(184);
            sprite.Client.SendMessage(0x02, "Your spell has been reflected!");

            if (_target is Aisling)
                _target.Client.SendMessage(0x02, $"You reflected {_skill.Template.Name}.");

            _target = Spell.SpellReflect(_target, sprite);
        }

        if (_target.SpellNegate)
        {
            _target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (_target is Aisling)
                _target.Client.SendMessage(0x02, $"You deflected {_skill.Template.Name}.");

            return;
        }

        var dmgCalc = DamageCalc(sprite);
        _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, _skill);
        aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
        _skillMethod.OnSuccess(_target, aisling, _skill, 0, false, action);
    }

    public override async void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, _skill);

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
            var enemy = sprite.MonsterGetInFront(5);

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;
            await SendAnimations(sprite, enemy);

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You reflected {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You deflected {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        foreach (var position in damageDealingSprite.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.First().Position)))
        {
            var vector = new Vector2(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(147, vector, 150));
            });
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
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