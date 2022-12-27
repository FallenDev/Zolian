using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("Ninja Star")]
public class NinjaStar : WeaponScript
{
    public NinjaStar(Item item) : base(item) { }

    public override void OnUse(Sprite sprite, Action<int> cb = null)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Ninja Star";

        var enemy = damageDealingSprite.DamageableGetInFront(4);
        var count = 1;
        if (enemy.Count == 0) return;

        foreach (var i in enemy)
        {
            if (i is not Monster or Aisling) continue;

            var animation = new ServerFormat29()
            {
                CasterSerial = (uint)damageDealingSprite.Serial,
                TargetSerial = (uint) i.Serial,
                CasterEffect = 10011,
                TargetEffect = 10011,
                Speed = 100
            };

            var dmg = damageDealingSprite.Dex * damageDealingSprite.Position.DistanceFrom(i.Position);
            dmg /= count;
                
            damageDealingSprite.Show(Scope.NearbyAislings, animation);
            i.ApplyDamage(damageDealingSprite, dmg, null);

            count++;
        }
    }
}