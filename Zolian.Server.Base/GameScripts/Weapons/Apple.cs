using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("Apple")]
public class Apple(Item item) : WeaponScript(item)
{
    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Rotten Apple";

        var enemy = damageDealingSprite.DamageableGetAwayInFront(4).FirstOrDefault();

        if (enemy is not Monster damageable) return;

        var dmg = damageDealingSprite.Dex * damageDealingSprite.Position.DistanceFrom(enemy.Position);
        // Rotten debuff
        dmg /= 2;
        damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(10010, null, enemy.Serial, 100, 10010, damageDealingSprite.Serial));
        damageable.ApplyDamage(damageDealingSprite, dmg, null);
    }
}