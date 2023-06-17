using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
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

        var animation = new ServerFormat29
        {
            CasterSerial = (uint)damageDealingSprite.Serial,
            TargetSerial = (uint)enemy.Serial,
            CasterEffect = 10010,
            TargetEffect = 10010,
            Speed = 100
        };

        var dmg = damageDealingSprite.Dex * damageDealingSprite.Position.DistanceFrom(enemy.Position);

        // Rotten debuff
        dmg /= 2;
            
        damageDealingSprite.Show(Scope.NearbyAislings, animation);
        enemy.ApplyDamage(damageDealingSprite, dmg, null);
    }
}