using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
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
        var equipmentSlot = Item.Template.EquipmentSlot;

        switch (equipmentSlot)
        {
            case 7 or 8: // Rings
                {
                    if (client.Aisling.EquipmentManager.Equipment[7] != null && client.Aisling.EquipmentManager.Equipment[8] != null)
                    {
                        equipmentSlot = aisling.LastRingSwap switch
                        {
                            7 => ItemSlots.RHand,
                            8 => ItemSlots.LHand,
                            _ => equipmentSlot
                        };

                        aisling.LastRingSwap = equipmentSlot;
                        break;
                    }

                    if (client.Aisling.EquipmentManager.Equipment[7] == null)
                        equipmentSlot = ItemSlots.LHand;
                    else if (client.Aisling.EquipmentManager.Equipment[8] == null)
                        equipmentSlot = ItemSlots.RHand;

                    aisling.LastRingSwap = equipmentSlot;
                }
                break;
            case 9 or 10: // Arms, Bracers, Gauntlets
                {
                    if (client.Aisling.EquipmentManager.Equipment[9] != null && client.Aisling.EquipmentManager.Equipment[10] != null)
                    {
                        equipmentSlot = aisling.LastHandSwap switch
                        {
                            9 => ItemSlots.RArm,
                            10 => ItemSlots.LArm,
                            _ => equipmentSlot
                        };

                        aisling.LastHandSwap = equipmentSlot;
                        break;
                    }

                    if (client.Aisling.EquipmentManager.Equipment[9] == null)
                        equipmentSlot = ItemSlots.LArm;
                    else if (client.Aisling.EquipmentManager.Equipment[10] == null)
                        equipmentSlot = ItemSlots.RArm;
                    aisling.LastHandSwap = equipmentSlot;
                }
                break;
            case 12: // Legs, ShinGuards
                if (client.Aisling.EquipmentManager.Equipment[12] == null)
                    equipmentSlot = ItemSlots.Leg;
                break;
            case 14: // First Accessory
                if (client.Aisling.EquipmentManager.Equipment[14] == null)
                    equipmentSlot = ItemSlots.FirstAcc;
                break;
            case 17: // Second Accessory
                if (client.Aisling.EquipmentManager.Equipment[17] == null)
                    equipmentSlot = ItemSlots.SecondAcc;
                break;
            case 18: // Third Accessory
                if (client.Aisling.EquipmentManager.Equipment[18] == null)
                    equipmentSlot = ItemSlots.ThirdAcc;
                break;
        }

        if (client.CheckReqs(client, Item))
            client.Aisling.EquipmentManager.Add(equipmentSlot, Item);
    }

    public override void Equipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        switch (Item.Template.EquipmentSlot)
        {
            case 12: // Legs, ShinGuards
                if (aisling.EquipmentManager.Equipment[12]?.Item?.Template.Name == "Anklet")
                {
                    client.Aisling.BootsImg = 8;
                    client.Aisling.BootColor = 0;
                }
                break;
            case 14: // First Accessory
                if (!Item.Template.Flags.FlagIsSet(ItemFlags.Elemental)) return;

                // Off-Hand elements override First Accessory
                if (aisling.EquipmentManager.Equipment[3]?.Item != null)
                    if (aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
                    {
                        if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                            aisling.SecondaryOffensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement;
                        else
                        {
                            if (Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                                aisling.SecondaryOffensiveElement = Item.Template.SecondaryOffensiveElement;
                        }

                        if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                            aisling.SecondaryDefensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement;
                        else
                        {
                            if (Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                                aisling.SecondaryDefensiveElement = Item.Template.SecondaryDefensiveElement;
                        }

                        return;
                    }

                if (Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                    aisling.SecondaryOffensiveElement = Item.Template.SecondaryOffensiveElement;
                if (Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                    aisling.SecondaryDefensiveElement = Item.Template.SecondaryDefensiveElement;
                break;
        }
    }

    public override void UnEquipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        switch (Item.Template.EquipmentSlot)
        {
            case 12: // Legs, ShinGuards
                if (aisling.EquipmentManager.Equipment[13]?.Item != null)
                {
                    client.Aisling.BootsImg = (short)aisling.EquipmentManager.Equipment[13].Item.Image;
                    client.Aisling.BootColor = (byte)aisling.EquipmentManager.Equipment[13].Item.Template.Color;
                }
                break;
            case 14: // First Accessory
                if (!Item.Template.Flags.FlagIsSet(ItemFlags.Elemental)) return;

                // Off-Hand elements override First Accessory
                if (aisling.EquipmentManager.Equipment[3]?.Item != null)
                    if (aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
                    {
                        if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                            aisling.SecondaryOffensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement;
                        else
                        {
                            if (Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                                aisling.SecondaryOffensiveElement = ElementManager.Element.None;
                        }

                        if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                            aisling.SecondaryDefensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement;
                        else
                        {
                            if (Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                                aisling.SecondaryDefensiveElement = ElementManager.Element.None;
                        }

                        return;
                    }

                aisling.SecondaryOffensiveElement = ElementManager.Element.None;
                aisling.SecondaryDefensiveElement = ElementManager.Element.None;
                break;
        }
    }
}