namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat68 : NetworkFormat
{
    /// <summary>
    /// Request Homepage
    /// </summary>
    public ClientFormat68()
    {
        Encrypted = true;
        Command = 0x68;
    }

    private byte Type { get; set; }

    public override void Serialize(NetworkPacketReader reader) => Type = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}