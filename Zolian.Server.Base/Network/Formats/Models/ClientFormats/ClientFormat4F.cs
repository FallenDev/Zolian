namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat4F : NetworkFormat
    {
        /// <summary>
        /// Player Portrait & Profile Message
        /// </summary>
        public ClientFormat4F()
        {
            Encrypted = true;
            Command = 0x4F;
        }

        private ushort Count { get; set; }
        public byte[] Image { get; set; }
        public string Words { get; set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Count = reader.ReadUInt16();
            Image = reader.ReadBytes(reader.ReadUInt16());
            Words = reader.ReadStringB();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}