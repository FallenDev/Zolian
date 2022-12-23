namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat3A : NetworkFormat
    {
        private readonly ushort _icon;
        private readonly byte _length;

        /// <summary>
        /// Status Bar
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="length"></param>
        public ServerFormat3A(ushort icon, byte length) : this()
        {
            _icon = icon;
            _length = length;
        }

        private ServerFormat3A()
        {
            Encrypted = true;
            Command = 0x3A;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(_icon);
            writer.Write(_length);
        }

        private enum IconStatus : ushort
        {
            Active = 0,
            Available = 266,
            Unavailable = 532
        }
    }
}