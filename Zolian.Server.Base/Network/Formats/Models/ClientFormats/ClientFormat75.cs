namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat75 : NetworkFormat
    {
        /// <summary>
        /// Tick Synchronization
        /// </summary>
        public ClientFormat75()
        {
            Encrypted = true;
            Command = 0x75;
        }

        public uint ServerTick;
        public uint ClientTick;

        public override void Serialize(NetworkPacketReader reader)
        {
            ServerTick = reader.ReadUInt32();
            ClientTick = reader.ReadUInt32();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}