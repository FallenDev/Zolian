namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat32 : NetworkFormat
    {
        public uint UnknownA;
        public uint UnknownB;
        public uint UnknownC;
        public uint UnknownD;

        /// <summary>
        /// Unknown
        /// </summary>
        public ClientFormat32()
        {
            Encrypted = true;
            Command = 0x32;
        }

        public override void Serialize(NetworkPacketReader reader)
        {
            UnknownA = reader.ReadUInt32();
            UnknownB = reader.ReadUInt32();
            UnknownC = reader.ReadUInt32();
            UnknownD = reader.ReadUInt32();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}