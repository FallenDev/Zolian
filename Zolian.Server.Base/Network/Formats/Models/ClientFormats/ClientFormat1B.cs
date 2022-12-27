namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat1B : NetworkFormat
{
    public int Index;

    /// <summary>
    /// User Option Toggle
    /// </summary>
    public ClientFormat1B()
    {
        Encrypted = true;
        Command = 0x1B;
    }

    public override void Serialize(NetworkPacketReader reader) => Index = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}