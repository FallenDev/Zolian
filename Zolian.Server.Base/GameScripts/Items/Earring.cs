using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Items
{
    [Script("Earring")]
    public class Earring : ItemScript
    {
        public Earring(Item item) : base(item) { }

        public override void Equipped(Sprite sprite, byte displaySlot)
        {
            if (sprite == null) return;
            if (Item?.Template == null) return;
            if (sprite is not Aisling aisling) return;
            var client = aisling.Client;
            if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

            Item.ApplyModifiers(client);
        }

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

        public override void UnEquipped(Sprite sprite, byte displaySlot)
        {
            if (sprite == null) return;
            if (Item?.Template == null) return;
            if (sprite is not Aisling aisling) return;
            var client = aisling.Client;
            if (!Item.Template.Flags.FlagIsSet(ItemFlags.Equipable)) return;

            Item.RemoveModifiers(client);
        }
    }
}