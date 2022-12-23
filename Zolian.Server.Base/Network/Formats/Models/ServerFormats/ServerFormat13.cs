namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat13 : NetworkFormat
    {
        public ushort Health;
        public int Serial;
        public byte Sound;

        /// <summary>
        /// Health bar
        /// </summary>
        public ServerFormat13(int serial, ushort health, byte sound) : this()
        {
            Serial = serial;
            Health = health;
            Sound = sound;
        }

        public ServerFormat13()
        {
            Encrypted = true;
            Command = 0x13;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Serial);
            writer.Write(Health);
            writer.Write(Sound);
        }
    }
}