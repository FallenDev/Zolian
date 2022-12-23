namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat57 : NetworkFormat
    {
        private byte _slot;
        public byte Type;

        /// <summary>
        /// Server Table Request
        /// </summary>
        public ClientFormat57()
        {
            Encrypted = true;
            Command = 0x57;
        }

        public override void Serialize(NetworkPacketReader reader)
        {
            Type = reader.ReadByte();
            if (reader.GetCanRead())
                _slot = reader.ReadByte();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}