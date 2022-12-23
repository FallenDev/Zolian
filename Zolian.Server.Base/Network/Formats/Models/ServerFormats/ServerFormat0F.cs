using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat0F : NetworkFormat
    {
        /// <summary>
        /// Add to Inventory
        /// </summary>
        /// <param name="item"></param>
        public ServerFormat0F(Item item) : this() => Item = item;

        private ServerFormat0F()
        {
            Encrypted = true;
            Command = 0x0F;
        }

        private Item Item { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Item.InventorySlot);
            writer.Write(Item.DisplayImage);
            writer.Write(Item.Color);
            writer.WriteStringA(Item.DisplayName);
            writer.Write((int)Item.Stacks);
            writer.Write(Item.Template.CanStack);
            writer.Write(Item.MaxDurability);
            writer.Write(Item.Durability);
            writer.Write((uint)0x00);
        }
    }
}