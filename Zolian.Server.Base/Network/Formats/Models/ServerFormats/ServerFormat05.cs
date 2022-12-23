using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat05 : NetworkFormat
    {
        private readonly Aisling _aisling;

        /// <summary>
        /// UserID (Serial), Direction, Rogue Map, Gender
        /// </summary>
        public ServerFormat05(Aisling aisling) : this() => _aisling = aisling;

        private ServerFormat05()
        {
            Encrypted = true;
            Command = 0x05;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(_aisling.Serial);
            writer.Write(_aisling.Direction);
            writer.Write((byte)0x00);
            writer.Write((byte)0x02); // Zoom Map
            writer.Write((byte)0x00);
            writer.Write((byte)_aisling.Gender);
            writer.Write((byte)0x00);
        }
    }
}