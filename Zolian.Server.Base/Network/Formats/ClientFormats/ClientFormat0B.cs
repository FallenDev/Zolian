namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat0B : NetworkFormat
{
    /// <summary>
    /// Exit Request
    /// </summary>
    public ClientFormat0B()
    {
        Encrypted = true;
        OpCode = 0x0B;
    }

    public byte Type { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Type = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}