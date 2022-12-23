namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat45 : NetworkFormat
    {
        /// <summary>
        /// Client Heartbeat Response
        /// </summary>
        public ClientFormat45()
        {
            Encrypted = true;
            Command = 0x45;
        }

        public DateTime Ping { get; private set; }
        public byte First { get; set; }
        public byte Second { get; set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Second = reader.ReadByte();
            First = reader.ReadByte();

            if (Second != 0x14) return;
            Ping = DateTime.Now;
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}