using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Items;

[Script("Generic")]
public class Generic : ItemScript
{
    public Generic(Item item) : base(item) { }

    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (Item.Template.EquipmentSlot is ItemSlots.LArm or ItemSlots.RArm)
            if (client.Aisling.EquipmentManager.Equipment[9] == null)
                Item.Template.EquipmentSlot = ItemSlots.LArm;
            else if (client.Aisling.EquipmentManager.Equipment[10] == null)
                Item.Template.EquipmentSlot = ItemSlots.RArm;
        if (Item.Template.EquipmentSlot is ItemSlots.LHand or ItemSlots.RHand)
            if (client.Aisling.EquipmentManager.Equipment[7] == null)
                Item.Template.EquipmentSlot = ItemSlots.LHand;
            else if (client.Aisling.EquipmentManager.Equipment[8] == null)
                Item.Template.EquipmentSlot = ItemSlots.RHand;
        if (Item.Template.EquipmentSlot is ItemSlots.Leg)
            if (client.Aisling.EquipmentManager.Equipment[12] == null)
                Item.Template.EquipmentSlot = ItemSlots.Leg;

        if (client.CheckReqs(client, Item))
            client.Aisling.EquipmentManager.Add(Item.Template.EquipmentSlot, Item);
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }
}