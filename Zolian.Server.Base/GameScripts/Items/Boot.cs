using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Items;

[Script("Boot")]
public class Boot(Item item) : ItemScript(item)
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

    public override void Equipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        var i = aisling.EquipmentManager.Equipment[12]?.Slot;
        if (i != null)
        {
            if (aisling.EquipmentManager.Equipment[12]?.Item?.Template.Name == "Anklet")
            {
                client.Aisling.BootsImg = 8;
                client.Aisling.BootColor = 0;
                return;
            }
        }

        client.Aisling.BootsImg = (short)Item.Image;
        client.Aisling.BootColor = (byte)Item.Template.Color;
    }

    public override void UnEquipped(Sprite sprite, byte displaySlot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;
        CalculateGearPoints(client);

        client.Aisling.BootsImg = 0;
        client.Aisling.BootColor = 0;
    }
}