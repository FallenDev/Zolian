namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat0E : NetworkFormat
    {
        /// <summary>
        /// Public Chat
        /// </summary>
        public enum MsgType : byte
        {
            Normal = 0,
            Shout = 1,
            Chant = 2
        }

        public ClientFormat0E()
        {
            Encrypted = true;
            Command = 0x0E;
        }

        public string Text { get; private set; }
        public byte Type { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Type = reader.ReadByte();
            Text = reader.ReadStringA();
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}