using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("IceQuiver")]
public class IceQuiver(Item item) : WeaponScript(item)
{
    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = damageDealingSprite.Serial
        };

        var enemy = damageDealingSprite.DamageableGetAwayInFront(6).FirstOrDefault();
        damageDealingSprite.ActionUsed = "Ice Quiver";

        switch (enemy)
        {
            case null:
            case not Monster:
                return;
        }

        var dmg = damageDealingSprite.Dex * 3 * Math.Max(damageDealingSprite.Position.DistanceFrom(enemy.Position), 5);
        // Rotten debuff
        dmg += dmg * 130 / 100;
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        Task.Run(async () =>
        {
            await Task.Delay(100);
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(10006, null, enemy.Serial, 100, 10006, damageDealingSprite.Serial));
        });

        Task.Run(async () =>
        {
            await Task.Delay(200);
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(10006, null, enemy.Serial, 100, 10006, damageDealingSprite.Serial));
        });

        enemy.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Water, null);
        enemy.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Water, null);
    }
}