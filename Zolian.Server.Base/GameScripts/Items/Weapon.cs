﻿using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Items;

[Script("Weapon")]
public class Weapon(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        // Unequip two-handed if already equipped
        if (aisling.EquipmentManager.Equipment[1]?.Item != null)
        {
            var checkTwoHand = aisling.EquipmentManager.Equipment[1].Item;
            if (checkTwoHand.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) ||
                checkTwoHand.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
            {
                aisling.EquipmentManager.RemoveFromExisting(1);
            }
        }

        // Bow skill check
        if (Item.Template.Flags.FlagIsSet(ItemFlags.LongRanged))
        {
            if (!client.Aisling.UseBows)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require a unique skill for this.");
                return;
            }
        }

        // Remove off-hand item if exists & Two-hand skill check
        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded))
        {
            if (!client.Aisling.TwoHandedBasher)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require a unique skill for this.");
                return;
            }

            var i = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (i != null && !aisling.EquipmentManager.RemoveFromExisting((int)i))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require both hands to equip such an item.");
                return;
            }
        }

        // Remove off-hand item if exists & Two-hand skill check
        if (Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
        {
            if (!client.Aisling.TwoHandedCaster)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require a unique skill for this.");
                return;
            }

            var i = aisling.EquipmentManager.Equipment[3]?.Slot;
            if (i != null && !aisling.EquipmentManager.RemoveFromExisting((int)i))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou require both hands to equip such an item.");
                return;
            }
        }

        // Dual wield skill check
        if (client.Aisling.DualWield && Item.Template.Flags.FlagIsSet(ItemFlags.DualWield))
        {
            if (client.Aisling.EquipmentManager.Equipment.TryGetValue(1, out var weaponSlot))
                if (weaponSlot?.Item != null)
                {
                    if (!weaponSlot.Item.Template.Flags.FlagIsSet(ItemFlags.LongRanged))
                    {
                        var mainHand = weaponSlot.Item;
                        if (mainHand.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || mainHand.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff))
                        {
                            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou couldn't possibly grip anything else.");
                            return;
                        }
                    }
                }

            if (!client.CheckReqs(client, Item)) return;

            int equipSlot;

            if (weaponSlot == null)
            {
                equipSlot = ItemSlots.Weapon;
            }
            else if (weaponSlot.Item == null)
            {
                equipSlot = ItemSlots.Weapon;
            }
            else
            {
                equipSlot = ItemSlots.Shield;
            }

            if (equipSlot == ItemSlots.Shield)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{Item.NoColorDisplayName} equipped to your off-hand.");
            }

            client.Aisling.EquipmentManager.Add(equipSlot, Item);
            return;
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
            client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }

        var templateImage = aisling.EquipmentManager.Equipment[1]?.Item.Template.Image;
        var offHandImage = aisling.EquipmentManager.Equipment[3]?.Item.Template.OffHandImage;

        if (templateImage != null)
            client.Aisling.WeaponImg = (short)templateImage;
        if (offHandImage != 0 && slot == 3)
            client.Aisling.ShieldImg = (short)Item.Template.OffHandImage;

        client.Aisling.UsingTwoHanded = Item.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) || Item.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff);
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (aisling.EquipmentManager.Equipment[1] == null && aisling.EquipmentManager.Equipment[3] == null)
        {
            client.Aisling.WeaponImg = 0;
            client.Aisling.ShieldImg = 0;
        }

        switch (slot)
        {
            case 1:
                client.Aisling.WeaponImg = 0;
                break;
            case 3 when aisling.EquipmentManager.Equipment[1] == null:
                client.Aisling.WeaponImg = 0;
                client.Aisling.ShieldImg = 0;
                break;
            case 3:
                client.Aisling.ShieldImg = 0;
                break;
        }

        client.Aisling.UsingTwoHanded = false;

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }
}