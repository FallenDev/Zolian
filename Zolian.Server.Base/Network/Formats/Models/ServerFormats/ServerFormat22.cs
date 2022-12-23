namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat22 : NetworkFormat
    {
        /// <summary>
        /// Server Refresh
        /// </summary>
        public ServerFormat22()
        {
            Encrypted = true;
            Command = 0x22;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) => writer.Write((byte)0x00);
    }
}