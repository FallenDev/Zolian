using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items;

[Script("Shield")]
public class Shield : ItemScript
{
    public Shield(Item item) : base(item) { }

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
                aisling.Client.SendMessage(0x02, "I cannot wield an offhand with this weapon.");
                return;
            }

            if (e.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                aisling.Client.SendMessage(0x02, "I cannot wield an offhand with a staff.");
                return;
            }
        }

        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded))
        {
            if (!client.Aisling.TwoHandedBasher)
            {
                aisling.Client.SendMessage(0x03, "{=cYou require a unique skill for this.");
                return;
            }

            var l = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (l != null && !aisling.EquipmentManager.RemoveFromExisting((int)l))
            {
                aisling.Client.SendMessage(0x03, "{=cYou require both hands to equip such an item.");
                return;
            }
        }

        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
        {
            if (!client.Aisling.TwoHandedCaster)
            {
                aisling.Client.SendMessage(0x03, "{=cYou require a unique skill for this.");
                return;
            }

            var k = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (k != null && !aisling.EquipmentManager.RemoveFromExisting((int)k))
            {
                aisling.Client.SendMessage(0x03, "{=cYou require both hands to equip such an item.");
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

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            client.Send(new ServerFormat17(spell));
        }

        if (aisling.EquipmentManager.Equipment[1] == null && Item.Template.Group == "Shields")
        {    
            client.Aisling.WeaponImg = ushort.MinValue;
            client.Aisling.ShieldImg = (byte)Item.Template.Image;
        }
        else
        {
            if (Item.Template.OffHandImage != 0)
                client.Aisling.ShieldImg = (byte)Item.Template.OffHandImage;
            else
                client.Aisling.ShieldImg = (byte)Item.Template.Image;
        }

        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Elemental)) return;
        aisling.SecondaryOffensiveElement = Item.Template.SecondaryOffensiveElement;
        aisling.SecondaryDefensiveElement = Item.Template.SecondaryDefensiveElement;
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        client.Aisling.ShieldImg = byte.MinValue;
        if (Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
        {
            aisling.SecondaryOffensiveElement = ElementManager.Element.None;
            aisling.SecondaryDefensiveElement = ElementManager.Element.None;
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            client.Send(new ServerFormat17(spell));
        }
    }
}