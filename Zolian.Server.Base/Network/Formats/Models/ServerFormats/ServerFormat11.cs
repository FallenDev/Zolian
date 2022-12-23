namespace Darkages.Network.Formats.Models.ServerFormats
{
    /// <summary>
    /// Sprite Direction
    /// </summary>
    public class ServerFormat11 : NetworkFormat
    {
        public byte Direction;
        public int Serial;

        public ServerFormat11()
        {
            Encrypted = true;
            Command = 0x11;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Serial);
            writer.Write(Direction);
        }
    }
}