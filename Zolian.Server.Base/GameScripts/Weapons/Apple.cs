using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("Apple")]
public class Apple : WeaponScript
{
    public Apple(Item item) : base(item) { }

    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Rotten Apple";

        var enemy = damageDealingSprite.DamageableGetAwayInFront(4).FirstOrDefault();

        switch (enemy)
        {
            case null:
            case not Monster:
                return;
        }

        var dmg = damageDealingSprite.Dex * damageDealingSprite.Position.DistanceFrom(enemy.Position);
        // Rotten debuff
        dmg /= 2;
        damageDealingSprite.Client.SendTargetedAnimation(Scope.NearbyAislings, 10010, 100, 10010, damageDealingSprite.Serial, enemy.Serial);
        enemy.ApplyDamage(damageDealingSprite, dmg, null);
    }
}