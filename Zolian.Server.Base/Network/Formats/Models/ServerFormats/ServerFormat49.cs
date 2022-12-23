namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat49 : NetworkFormat
    {
        /// <summary>
        /// Request Portrait
        /// </summary>
        public ServerFormat49()
        {
            Encrypted = true;
            Command = 0x49;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) => writer.Write(byte.MinValue);
    }
}