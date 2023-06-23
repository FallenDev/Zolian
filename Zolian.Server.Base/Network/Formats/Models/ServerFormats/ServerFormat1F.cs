namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat1F : NetworkFormat
{
    private const byte Type = 0x03;

    /// <summary>
    /// Map Change Completed
    /// </summary>
    public ServerFormat1F()
    {
        Encrypted = true;
        OpCode = 0x1F;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Type);
        writer.Write(ushort.MinValue);
    }
}