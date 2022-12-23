namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat0A : NetworkFormat
    {
        private enum MsgType : byte
        {
            Whisper = 0x00,
            Action1 = 0x01,
            Action2 = 0x02,
            Global = 0x03,
            Action3 = 0x04,
            GameMaster = 0x05,
            Action4 = 0x06,
            UserOptions = 0x07,
            ScrollWindow = 0x08,
            NonScrollWindow = 0x09,
            WoodenBoard = 0x0A,
            GroupChat = 0x0B,
            GuildChat = 0x0C,
            ClosePopup = 0x11,
            TopRight = 0x12
        }

        /// <summary>
        /// System Message
        /// </summary>
        /// <param name="type"></param>
        /// <param name="text"></param>
        public ServerFormat0A(byte type, string text) : this()
        {
            Type = type;
            Text = text;
        }

        private ServerFormat0A()
        {
            Encrypted = true;
            Command = 0x0A;
        }

        private string Text { get; }
        private byte Type { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);

            if (!string.IsNullOrEmpty(Text))
                writer.WriteStringB(Text);
        }
    }
}