namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat06 : NetworkFormat
    {
        /// <summary>
        /// Client Movement
        /// </summary>
        public ClientFormat06()
        {
            Encrypted = true;
            Command = 0x06;
        }

        public byte Direction { get; private set; }
        private byte StepCount { get; set; }
        
        public override void Serialize(NetworkPacketReader reader)
        {
            Direction = reader.ReadByte();
            StepCount = reader.ReadByte();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}