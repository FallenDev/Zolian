namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat3E : NetworkFormat
    {
        /// <summary>
        /// Skill Use
        /// </summary>
        public ClientFormat3E()
        {
            Encrypted = true;
            Command = 0x3E;
        }

        public byte Index { get; private set; }

        public override void Serialize(NetworkPacketReader reader) => Index = reader.ReadByte();

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}