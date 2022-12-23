namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat02 : NetworkFormat
    {
        /// <summary>
        /// Login Message
        /// </summary>
        /// <param name="code"></param>
        /// <param name="text"></param>
        public ServerFormat02(byte code, string text)
        {
            Encrypted = true;
            Command = 0x02;
            Code = code;
            Text = text;
        }

        private byte Code { get; }
        private string Text { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Code);
            writer.WriteStringA(Text);
        }
    }
}