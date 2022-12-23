namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat18 : NetworkFormat
    {
        /// <summary>
        /// Request World List
        /// </summary>
        public ClientFormat18()
        {
            Encrypted = true;
            Command = 0x18;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}