using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items
{
    [Script("Helmet")]
    public class Helmet : ItemScript
    {
        public Helmet(Item item) : base(item) { }

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

            Item.ApplyModifiers(client);

            client.Aisling.HelmetImg = Item.Image;
        }

        public override void UnEquipped(Sprite sprite, byte displaySlot)
        {
            if (sprite == null) return;
            if (Item?.Template == null) return;
            if (sprite is not Aisling aisling) return;
            var client = aisling.Client;
            if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

            client.Aisling.HelmetImg = ushort.MinValue;

            Item.RemoveModifiers(client);
        }
    }
}