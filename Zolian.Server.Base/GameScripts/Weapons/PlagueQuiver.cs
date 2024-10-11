using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Weapons;

[Script("PlagueQuiver")]
public class PlagueQuiver(Item item) : WeaponScript(item)
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
        damageDealingSprite.ActionUsed = "Plague Quiver";

        switch (enemy)
        {
            case null:
            case not Monster:
                return;
        }

        var dmg = damageDealingSprite.Dex * 3 * Math.Max(damageDealingSprite.Position.DistanceFrom(enemy.Position), 5);
        // Rotten debuff
        dmg += dmg * 160 / 100;
        damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed, action.Sound));
        Task.Run(async () =>
        {
            await Task.Delay(100);
            damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(10000, null, enemy.Serial, 100, 10000, damageDealingSprite.Serial));
        });
        var debuff = new DebuffMorPoison();
        damageDealingSprite.Client.EnqueueDebuffAppliedEvent(enemy, debuff);
        enemy.ApplyElementalSkillDamage(damageDealingSprite, dmg, ElementManager.Element.Earth, null);
    }
}