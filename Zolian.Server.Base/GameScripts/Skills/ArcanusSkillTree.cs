using System.Numerics;

using Darkages.Common;
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
    private const int CRIT_DMG = 3;
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
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override async void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Flame Thrower";

        if (damageDealingSprite.CurrentMp - 300 > 0)
        {
            damageDealingSprite.CurrentMp -= 300;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = client.Aisling.DamageableGetInFront(6);
        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(client, damageDealingSprite, _skill, action);
            OnFailed(damageDealingSprite);
            return;
        }

        await SendAnimations(damageDealingSprite, enemy);

        _target = enemy.First();

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
        _target.ApplyElementalSkillDamage(damageDealingSprite, dmgCalc, ElementManager.Element.Fire, _skill);
        damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
        _skill.LastUsedSkill = DateTime.Now;
        _skillMethod.Train(client, _skill);
        damageDealingSprite.Show(Scope.NearbyAislings, action);
        if (!_crit) return;
        damageDealingSprite.Animate(387);
        _crit = false;
    }

    public override void OnUse(Sprite sprite)
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
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;

            var imp = 10 + _skill.Level;
            var dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);

            dmg += dmg * imp / 100;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;

            var dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;

            var critRoll = Generator.RandNumGen100();
            {
                if (critRoll < 95) return dmg;
                dmg *= CRIT_DMG;
                _crit = true;
            }

            return dmg;
        }
    }
}