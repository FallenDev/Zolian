﻿using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Weapons;

[Script("IceQuiver")]
public class IceQuiver(Item item) : WeaponScript(item)
{
    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        if (damageDealingSprite.EquipmentManager.Weapon?.Item == null) return;
        if (!damageDealingSprite.EquipmentManager.Weapon.Item.Template.Flags.FlagIsSet(ItemFlags.LongRanged)) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = damageDealingSprite.Serial
        };

        var enemy = damageDealingSprite.DamageableGetAwayInFront(6).FirstOrDefault();
        damageDealingSprite.ActionUsed = "Ice Quiver";

        if (enemy is not Monster damageable) return;

        var dmg = damageDealingSprite.Dex * 3 * Math.Max(damageDealingSprite.Position.DistanceFrom(enemy.Position), 5);
        // Rotten debuff
        dmg += dmg * 130 / 100;
        damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        Task.Run(async () =>
        {
            await Task.Delay(100);
            damageDealingSprite.SendAnimationNearby(10006, null, enemy.Serial, 100, 10006, damageDealingSprite.Serial);
        });

        Task.Run(async () =>
        {
            await Task.Delay(200);
            damageDealingSprite.SendAnimationNearby(10006, null, enemy.Serial, 100, 10006, damageDealingSprite.Serial);
        });

        damageable.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Wind, null);
        damageable.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Water, null);
    }
}