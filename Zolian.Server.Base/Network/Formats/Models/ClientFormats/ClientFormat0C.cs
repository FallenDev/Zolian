namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat0C : NetworkFormat
{
    /// <summary>
    /// Display Object Request
    /// </summary>
    public ClientFormat0C()
    {
        Encrypted = true;
        Command = 0x0C;
    }

    public override void Serialize(NetworkPacketReader reader) => reader.ReadUInt32();

    public override void Serialize(NetworkPacketWriter writer) { }
}