namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat31 : NetworkFormat
    {
        /// <summary>
        /// Board -- Logic handled outside of ServerFormat
        /// </summary>
        public ServerFormat31()
        {
            Encrypted = true;
            Command = 0x31;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}