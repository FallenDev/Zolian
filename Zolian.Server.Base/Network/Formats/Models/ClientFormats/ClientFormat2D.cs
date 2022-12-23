namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat2D : NetworkFormat
    {
        /// <summary>
        /// Request Player Profile, Load Character Data
        /// </summary>
        public ClientFormat2D()
        {
            Encrypted = true;
            Command = 0x2D;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}