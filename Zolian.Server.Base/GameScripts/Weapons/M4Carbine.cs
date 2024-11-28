using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Weapons;

[Script("M4Carbine")]
public class M4Carbine(Item item) : WeaponScript(item)
{
    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        if (!damageDealingSprite.EquipmentManager.Weapon.Item.Template.Flags.FlagIsSet(ItemFlags.LongRanged)) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.Assail,
            Sound = null,
            SourceId = damageDealingSprite.Serial
        };

        var enemy = damageDealingSprite.DamageableGetAwayInFront(9).FirstOrDefault();
        damageDealingSprite.ActionUsed = "pew pew pew!";

        if (enemy is not Monster damageable) return;

        var dmg = damageDealingSprite.Dex * 2 / Math.Max(damageDealingSprite.Position.DistanceFrom(enemy.Position), 9);
        // Gravity debuff
        dmg += dmg * 110 / 100;
        damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        Task.Run(async () =>
        {
            await Task.Delay(50);
            damageDealingSprite.SendAnimationNearby(10013, null, enemy.Serial, 20, 10001, damageDealingSprite.Serial);
        });

        Task.Run(async () =>
        {
            await Task.Delay(90);
            damageDealingSprite.SendAnimationNearby(10013, null, enemy.Serial, 20, 10001, damageDealingSprite.Serial);
        });

        Task.Run(async () =>
        {
            await Task.Delay(130);
            damageDealingSprite.SendAnimationNearby(10013, null, enemy.Serial, 20, 10001, damageDealingSprite.Serial);
        });

        Task.Run(async () =>
        {
            await Task.Delay(170);
            damageDealingSprite.SendAnimationNearby(10013, null, enemy.Serial, 20, 10001, damageDealingSprite.Serial);
        });

        damageable.ApplyDamage(damageDealingSprite, dmg, null);
        damageable.ApplyDamage(damageDealingSprite, dmg, null);
        damageable.ApplyDamage(damageDealingSprite, dmg, null);
        damageable.ApplyDamage(damageDealingSprite, dmg, null);
    }
}