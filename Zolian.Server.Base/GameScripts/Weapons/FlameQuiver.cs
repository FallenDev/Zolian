using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("FlameQuiver")]
public class FlameQuiver(Item item) : WeaponScript(item)
{
    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        if (!damageDealingSprite.EquipmentManager.Weapon.Item.Template.Flags.FlagIsSet(ItemFlags.LongRanged)) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = damageDealingSprite.Serial
        };

        var enemy = damageDealingSprite.DamageableGetAwayInFront(6).FirstOrDefault();
        damageDealingSprite.ActionUsed = "Flame Quiver";

        if (enemy is not Monster damageable) return;

        var dmg = damageDealingSprite.Dex * 3 * Math.Max(damageDealingSprite.Position.DistanceFrom(enemy.Position), 5);
        // Rotten debuff
        dmg += dmg * 130 / 100;
        damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        Task.Run(async () =>
        {
            await Task.Delay(100);
            damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(10003, null, enemy.Serial, 100, 10003, damageDealingSprite.Serial));
        });

        Task.Run(async () =>
        {
            await Task.Delay(200);
            damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(10003, null, enemy.Serial, 100, 10003, damageDealingSprite.Serial));
        });

        damageable.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Fire, null);
        damageable.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Earth, null);
    }
}