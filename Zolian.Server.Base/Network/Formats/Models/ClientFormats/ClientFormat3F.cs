namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat3F : NetworkFormat
{
    public int Index;

    /// <summary>
    /// World Map Click
    /// </summary>
    public ClientFormat3F()
    {
        Encrypted = true;
        OpCode = 0x3F;
    }

    public override void Serialize(NetworkPacketReader reader) => Index = reader.ReadInt32();

    public override void Serialize(NetworkPacketWriter writer) { }
}