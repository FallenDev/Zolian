using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Items;

[Script("FirstAcc")]
public class FirstAcc(Item item) : ItemScript(item)
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

        client.Aisling.Accessory1Img = (short)Item.Image;
        client.Aisling.Accessory1Color = Item.Color;

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
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        client.Aisling.Accessory1Img = 0;
        client.Aisling.Accessory1Color = 0;

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
    }
}