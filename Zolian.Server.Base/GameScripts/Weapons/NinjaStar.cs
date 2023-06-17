using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("Ninja Star")]
public class NinjaStar : WeaponScript
{
    private Item _item;

    public NinjaStar(Item item) : base(item)
    {
        _item = item;
    }

    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Ninja Star";

        var enemy = damageDealingSprite.DamageableGetInFront(5).FirstOrDefault();

        switch (enemy)
        {
            case null:
            case not Monster or Aisling:
                return;
        }

        var animation = new ServerFormat29
        {
            CasterSerial = (uint)damageDealingSprite.Serial,
            TargetSerial = (uint)enemy.Serial,
            CasterEffect = 10011,
            TargetEffect = 10011,
            Speed = 100
        };

        var dmg = damageDealingSprite.Dex * damageDealingSprite.Position.DistanceFrom(enemy.Position);

        switch (_item.ItemQuality)
        {
            // Dull debuff
            case Item.Quality.Damaged:
                dmg /= 2;
                break;
            // Sharpened buff
            case Item.Quality.Epic or Item.Quality.Legendary:
                dmg *= 2;
                break;
            // Razor sharp buff
            case Item.Quality.Forsaken:
                dmg *= 3;
                break;
            // God tip buff
            case Item.Quality.Mythic:
                dmg *= 4;
                break;
        }

        damageDealingSprite.Show(Scope.NearbyAislings, animation);
        enemy.ApplyDamage(damageDealingSprite, dmg, null);
    }
}