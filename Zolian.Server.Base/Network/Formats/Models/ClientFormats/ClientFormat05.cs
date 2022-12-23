namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat05 : NetworkFormat
    {
        /// <summary>
        /// Request Map Data
        /// </summary>
        public ClientFormat05()
        {
            Encrypted = true;
            Command = 0x05;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}