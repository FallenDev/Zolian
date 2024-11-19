using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Items;

[Script("Armor")]
public class Armor(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (client.CheckReqs(client, Item))
            client.Aisling.EquipmentManager.Add(Item.Template.EquipmentSlot, Item);
    }

    public override void Equipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);
        DetermineCastBodyAnimation(aisling, Item);
        DetermineMeleeBodyAnimations(aisling, Item);

        client.Aisling.Pants = (byte)(Item.Template.HasPants ? 1 : 0);
        client.Aisling.ArmorImg = (short)Item.Image;
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        client.Aisling.Pants = 0;
        client.Aisling.ArmorImg = 0;
    }

    private static void DetermineCastBodyAnimation(Aisling player, Item item)
    {
        player.CastBodyAnimation = CastAnimations(item);
    }

    private static void DetermineMeleeBodyAnimations(Aisling player, Item item)
    {
        player.MeleeBodyAnimation = MeleeAnimations(item);
    }

    private static byte CastAnimations(Item item)
    {
        switch (item.Template.AnimFlags)
        {
            case ArmorAnimationFlags.Peasant:
            case ArmorAnimationFlags.Warrior:
            case ArmorAnimationFlags.Rogue:
            case ArmorAnimationFlags.Monk:
            case ArmorAnimationFlags.Gladiator:
            case ArmorAnimationFlags.Druid:
                return 6;
            case ArmorAnimationFlags.Wizard:
                return 136;
            case ArmorAnimationFlags.Priest:
                return 128;
            case ArmorAnimationFlags.Archer:
                return 144;
            case ArmorAnimationFlags.Summoner:
                return 145;
            case ArmorAnimationFlags.Bard:
                return 137;
        }

        return 6;
    }

    private static byte MeleeAnimations(Item item)
    {
        switch (item.Template.AnimFlags)
        {
            case ArmorAnimationFlags.Peasant:
            case ArmorAnimationFlags.Warrior:
            case ArmorAnimationFlags.Wizard:
            case ArmorAnimationFlags.Priest:
            case ArmorAnimationFlags.Summoner:
            case ArmorAnimationFlags.Bard:
                return 1;
            case ArmorAnimationFlags.Rogue:
                return 134;
            case ArmorAnimationFlags.Monk:
                return 132;
            case ArmorAnimationFlags.Gladiator:
                return 139;
            case ArmorAnimationFlags.Archer:
                return 142;
            case ArmorAnimationFlags.Druid:
                return 133;
        }

        return 1;
    }
}