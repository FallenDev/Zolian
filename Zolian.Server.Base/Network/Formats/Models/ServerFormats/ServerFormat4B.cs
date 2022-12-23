namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat4B : NetworkFormat
    {
        /// <summary>
        /// Bounce
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="type"></param>
        /// <param name="itemSlot"></param>
        public ServerFormat4B(uint serial, byte type, byte itemSlot = 0) : this()
        {
            Type = type;
            Serial = serial;
            ItemSlot = itemSlot;
        }

        private ServerFormat4B()
        {
            Encrypted = true;
            Command = 0x4B;
        }

        private byte ItemSlot { get; }
        private uint Serial { get; }
        private byte Type { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (Type == 0)
            {
                writer.Write((ushort)0x06);
                writer.Write((byte)0x4A);
                writer.Write((byte)0x00);
                writer.Write(Serial);
                writer.Write((byte)0x00);
            }

            if (Type != 1) return;
            writer.Write((ushort)0x07);
            writer.Write((byte)0x4A);
            writer.Write((byte)0x01);
            writer.Write(Serial);
            writer.Write(ItemSlot);
            writer.Write((byte)0x00);
        }
    }
}