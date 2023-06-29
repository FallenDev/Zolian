namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat1C : NetworkFormat
{
    /// <summary>
    /// Item Usage
    /// </summary>
    public ClientFormat1C()
    {
        Encrypted = true;
        OpCode = 0x1C;
    }

    public byte Index { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Index = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}