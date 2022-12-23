namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat66 : NetworkFormat
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public ClientFormat66()
        {
            Encrypted = true;
            Command = 0x66;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}