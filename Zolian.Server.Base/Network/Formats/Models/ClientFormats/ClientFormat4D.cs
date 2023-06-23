namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat4D : NetworkFormat
{
    /// <summary>
    /// Begin Casting
    /// </summary>
    public ClientFormat4D()
    {
        Encrypted = true;
        OpCode = 0x4D;
    }

    public byte Lines { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Lines = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}