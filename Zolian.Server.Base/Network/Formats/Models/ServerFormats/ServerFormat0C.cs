namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat0C : NetworkFormat
    {
        public byte Direction;
        public int Serial;
        public short X;
        public short Y;

        /// <summary>
        /// Monster Move
        /// </summary>
        public ServerFormat0C()
        {
            Encrypted = true;
            Command = 0x0C;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Serial);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Direction);
            writer.Write((byte)0x00);
        }
    }
}