namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat00 : NetworkFormat
{
    public int Version;

    /// <summary>
    /// Connection Information Request
    /// </summary>
    public ClientFormat00()
    {
        Encrypted = false;
        OpCode = 0x00;
    }

    public override void Serialize(NetworkPacketReader reader)
    {
        Version = reader.ReadUInt16();
        reader.ReadByte();
        reader.ReadByte();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}