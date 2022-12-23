namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat18 : NetworkFormat
    {
        private readonly byte _slot;

        /// <summary>
        /// Remove Spell
        /// </summary>
        /// <param name="slot"></param>
        public ServerFormat18(byte slot) : this() => _slot = slot;

        private ServerFormat18()
        {
            Encrypted = true;
            Command = 0x18;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer) => writer.Write(_slot);
    }
}