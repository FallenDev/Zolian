namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat67 : NetworkFormat
    {
        private const byte Type = 0x03;

        /// <summary>
        /// Map Change Pending
        /// </summary>
        public ServerFormat67()
        {
            Encrypted = true;
            Command = 0x67;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);
            writer.Write(uint.MinValue);
        }
    }
}