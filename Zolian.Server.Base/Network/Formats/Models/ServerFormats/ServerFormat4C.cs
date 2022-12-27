namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat4C : NetworkFormat
{
    /// <summary>
    /// Reconnect
    /// </summary>
    public ServerFormat4C()
    {
        Encrypted = true;
        Command = 0x4C;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write((byte)0x01);
        writer.Write(byte.MinValue);
        writer.Write(byte.MinValue);
    }
}