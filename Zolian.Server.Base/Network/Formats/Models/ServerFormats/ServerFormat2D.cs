namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat2D : NetworkFormat
    {
        private readonly byte _slot;

        /// <summary>
        /// Remove Skill
        /// </summary>
        /// <param name="slot"></param>
        public ServerFormat2D(byte slot) : this() => _slot = slot;

        private ServerFormat2D()
        {
            Command = 0x2D;
            Encrypted = true;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) => writer.Write(_slot);
    }
}