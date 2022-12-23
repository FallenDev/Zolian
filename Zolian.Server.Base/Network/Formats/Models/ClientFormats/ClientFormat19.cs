namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat19 : NetworkFormat
    {
        /// <summary>
        /// Private Message
        /// </summary>
        public ClientFormat19()
        {
            Encrypted = true;
            Command = 0x19;
        }

        public string Message { get; private set; }
        public string Name { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Name = reader.ReadStringA();
            Message = reader.ReadStringA();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}