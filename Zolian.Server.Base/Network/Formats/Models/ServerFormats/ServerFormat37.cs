using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat37 : NetworkFormat
    {
        /// <summary>
        /// Add Equipment
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slot"></param>
        public ServerFormat37(Item item, byte slot) : this()
        {
            Item = item;
            EquipmentSlot = slot;
        }

        private ServerFormat37()
        {
            Encrypted = true;
            Command = 0x37;
        }

        private byte EquipmentSlot { get; }
        private Item Item { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(EquipmentSlot);
            writer.Write(Item.DisplayImage);
            writer.Write(Item.Color);
            writer.WriteStringA(Item.NoColorDisplayName);
            writer.WriteStringA(Item.NoColorDisplayName);
            writer.Write(Item.Durability);
            writer.Write(Item.MaxDurability);
        }
    }
}