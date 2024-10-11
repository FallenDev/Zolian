using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items;

[Script("Shield")]
public class Shield(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        var i = aisling.EquipmentManager.Equipment[1]?.Slot;
        if (i != null)
        {
            var e = aisling.EquipmentManager.Equipment[1].Item;

            if (e.Template.Flags.FlagIsSet(ItemFlags.TwoHanded))
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I cannot wield an offhand with this weapon.");
                return;
            }

            if (e.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "I cannot wield an offhand with a staff.");
                return;
            }
        }

        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded))
        {
            if (!client.Aisling.TwoHandedBasher)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require a unique skill for this.");
                return;
            }

            var l = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (l != null && !aisling.EquipmentManager.RemoveFromExistingSlot((int)l))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require both hands to equip such an item.");
                return;
            }
        }

        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
        {
            if (!client.Aisling.TwoHandedCaster)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require a unique skill for this.");
                return;
            }

            var k = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (k != null && !aisling.EquipmentManager.RemoveFromExistingSlot((int)k))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require both hands to equip such an item.");
                return;
            }
        }

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

        if (aisling.EquipmentManager.Equipment[1] == null && Item.Template.Group == "Shields")
        {
            client.Aisling.WeaponImg = 0;
            client.Aisling.ShieldImg = (short)Item.Template.Image;
        }
        else
        {
            if (Item.Template.OffHandImage != 0)
                client.Aisling.ShieldImg = (short)Item.Template.OffHandImage;
            else
                client.Aisling.ShieldImg = (short)Item.Template.Image;
        }

        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Elemental)) return;
        aisling.SecondaryOffensiveElement = Item.Template.SecondaryOffensiveElement;
        aisling.SecondaryDefensiveElement = Item.Template.SecondaryDefensiveElement;
        CalculateGearPoints(client);
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        client.Aisling.ShieldImg = short.MinValue;
        if (Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
        {
            aisling.SecondaryOffensiveElement = ElementManager.Element.None;
            aisling.SecondaryDefensiveElement = ElementManager.Element.None;

            // If First Accessory is elemental, set it
            if (aisling.EquipmentManager.Equipment[14]?.Item != null)
                if (aisling.EquipmentManager.Equipment[14].Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
                {
                    aisling.SecondaryOffensiveElement = aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryOffensiveElement;
                    aisling.SecondaryDefensiveElement = aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryDefensiveElement;
                    return;
                }
        }
        
        CalculateGearPoints(client);
    }
}