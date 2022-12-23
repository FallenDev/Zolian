namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat2A : NetworkFormat
    {
        public uint ID;
        public uint Gold;

        /// <summary>
        /// Gold Dropped on Sprite
        /// </summary>
        public ClientFormat2A()
        {
            Encrypted = true;
            Command = 0x2A;
        }

        public override void Serialize(NetworkPacketReader reader)
        {
            Gold = reader.ReadUInt32();
            ID = reader.ReadUInt32();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}