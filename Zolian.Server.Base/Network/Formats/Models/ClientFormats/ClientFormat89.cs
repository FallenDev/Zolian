namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat89 : NetworkFormat
    {
        /// <summary>
        /// Display Mask
        /// </summary>
        public ClientFormat89()
        {
            Encrypted = true;
            Command = 0x89;
        }

        private ushort DisplayMask { get; set; }

        public override void Serialize(NetworkPacketReader reader) => DisplayMask = reader.ReadUInt16();

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}