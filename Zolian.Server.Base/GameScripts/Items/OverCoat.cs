using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items;

[Script("OverCoat")]
public class OverCoat(Item item) : ItemScript(item)
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

        if (Item.Template.Name == "Onion Knight")
        {
            client.Aisling.HeadAccessoryImg = 0;
            client.Aisling.OldStyle = client.Aisling.HairStyle;
            client.Aisling.HairStyle = 100;
        }

        client.Aisling.OverCoatImg = (short)Item.Image;
        client.Aisling.OverCoatColor = Item.Color;
    }

    public override void UnEquipped(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

        if (Item.Template.Name == "Onion Knight")
        {
            client.Aisling.HairStyle = client.Aisling.OldStyle;
        }

        client.Aisling.OverCoatImg = 0;
        client.Aisling.OverCoatColor = 0;
    }
}