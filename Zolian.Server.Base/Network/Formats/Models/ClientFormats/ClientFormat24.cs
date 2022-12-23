namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat24 : NetworkFormat
    {
        /// <summary>
        /// Drop Gold
        /// </summary>
        public ClientFormat24()
        {
            Encrypted = true;
            Command = 0x24;
        }

        public uint GoldAmount { get; private set; }
        private short Unknown { get; set; }
        public short X { get; private set; }
        public short Y { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            GoldAmount = (uint)reader.ReadInt32();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();

            if (reader.GetCanRead())
                Unknown = reader.ReadInt16();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}