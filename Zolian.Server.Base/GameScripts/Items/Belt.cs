using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items;

[Script("Belt")]
public class Belt : ItemScript
{
    public Belt(Item item) : base(item) { }
        
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

    public override void Equipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling) return;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
            sprite.DefenseElement = Item.Template.DefenseElement;
    }

    public override void UnEquipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling) return;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
            sprite.DefenseElement = ElementManager.Element.None;
    }
}