using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Items;

/// <summary>
/// Generic Script is used when the item does not have a display image or any special logic
/// </summary>
[Script("Generic")]
public class Generic(Item item) : ItemScript(item)
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

        switch (Item.Template.EquipmentSlot)
        {
            case 7 or 8: // Rings
                if (client.Aisling.EquipmentManager.Equipment[7] == null)
                    Item.Template.EquipmentSlot = ItemSlots.LHand;
                else if (client.Aisling.EquipmentManager.Equipment[8] == null)
                    Item.Template.EquipmentSlot = ItemSlots.RHand;
                break;
            case 9 or 10: // Arms, Bracers, Gauntlets
                if (client.Aisling.EquipmentManager.Equipment[9] == null)
                    Item.Template.EquipmentSlot = ItemSlots.LArm;
                else if (client.Aisling.EquipmentManager.Equipment[10] == null)
                    Item.Template.EquipmentSlot = ItemSlots.RArm;
                break;
            case 12: // Legs, Guards, Boots
                if (client.Aisling.EquipmentManager.Equipment[12] == null)
                    Item.Template.EquipmentSlot = ItemSlots.Leg;
                if (aisling.EquipmentManager.Equipment[12].Item.Template.Name == "Anklet")
                {
                    client.Aisling.BootsImg = 8;
                }
                break;
            case 14: // First Accessory
                if (client.Aisling.EquipmentManager.Equipment[14] == null)
                    Item.Template.EquipmentSlot = ItemSlots.FirstAcc;
                break;
            case 17: // Second Accessory
                if (client.Aisling.EquipmentManager.Equipment[17] == null)
                    Item.Template.EquipmentSlot = ItemSlots.SecondAcc;
                break;
            case 18: // Third Accessory
                if (client.Aisling.EquipmentManager.Equipment[18] == null)
                    Item.Template.EquipmentSlot = ItemSlots.ThirdAcc;
                break;
        }
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }
}