using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Gender = Darkages.Enums.Gender;

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
        CalculateGearPoints(client);

        if (Item.Template.Name == "Onion Knight")
        {
            client.Aisling.HeadAccessoryImg = 0;
            client.Aisling.OldStyle = client.Aisling.HairStyle;
            client.Aisling.HairStyle = 100;
        }

        if (Item.Template.Name == "Sleigh Mount")
        {
            client.Aisling.MonsterForm = (ushort)(client.Aisling.Gender == Gender.Male ? 737 : 738);
            client.Aisling.OverCoatImg = 0;
            client.Aisling.OverCoatColor = 0;
            return;
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
        CalculateGearPoints(client);

        if (Item.Template.Name == "Onion Knight")
        {
            client.Aisling.HairStyle = client.Aisling.OldStyle;
        }

        if (Item.Template.Name == "Sleigh Mount")
        {
            client.Aisling.MonsterForm = 0;
        }

        client.Aisling.OverCoatImg = 0;
        client.Aisling.OverCoatColor = 0;
    }
}