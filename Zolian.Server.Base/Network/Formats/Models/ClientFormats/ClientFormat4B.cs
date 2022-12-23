namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat4B : NetworkFormat
    {
        /// <summary>
        /// Request Notice
        /// </summary>
        public ClientFormat4B()
        {
            Encrypted = true;
            Command = 0x4B;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}