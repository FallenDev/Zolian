namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat47 : NetworkFormat
{
    /// <summary>
    /// Stat Raised
    /// </summary>
    public ClientFormat47()
    {
        Encrypted = true;
        Command = 0x47;
    }

    public byte Stat { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Stat = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}