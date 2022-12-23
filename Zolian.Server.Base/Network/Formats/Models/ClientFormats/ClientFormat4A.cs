namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat4A : NetworkFormat
    {
        /// <summary>
        /// Client Exchange -Item Gold barter-
        /// </summary>
        public ClientFormat4A()
        {
            Encrypted = true;
            Command = 0x4A;
        }

        public uint Gold { get; set; }
        public uint Id { get; set; }
        public byte ItemSlot { get; private set; }
        public int Quantity { get; private set; }
        public byte Type { get; set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Type = reader.ReadByte();
            Id = reader.ReadUInt32();

            switch (Type)
            {
                case 0x00:
                    break;
                case 0x01 when reader.GetCanRead():
                    ItemSlot = reader.ReadByte();
                    Quantity = 1;
                    break;
                case 0x02 when reader.GetCanRead():
                    ItemSlot = reader.ReadByte();
                    Quantity = reader.ReadByte();
                    break;
                case 0x03 when reader.GetCanRead():
                    Gold = (uint)reader.ReadInt32();
                    break;
                case 0x04:
                case 0x05:
                    break;
            }
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}