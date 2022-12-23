namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat0D : NetworkFormat
    {
        public int Serial;
        public string Text;
        public byte Type;

        /// <summary>
        /// Spell Lines / Chat
        /// </summary>
        public ServerFormat0D()
        {
            Encrypted = true;
            Command = 0x0D;
        }

        public enum MsgType : byte
        {
            Chant = 2,
            Normal = 0,
            Shout = 1
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);
            writer.Write(Serial);
            writer.WriteStringA(Text);
        }
    }
}