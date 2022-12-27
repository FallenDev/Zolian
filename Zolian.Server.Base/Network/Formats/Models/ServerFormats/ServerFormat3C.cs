namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat3C : NetworkFormat
{
    public byte[] Data;
    public ushort Line;

    /// <summary>
    /// Map Data
    /// </summary>
    public ServerFormat3C()
    {
        Encrypted = true;
        Command = 0x3C;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Line);
        writer.Write(Data);
    }
}