namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat1D : NetworkFormat
{
    public byte Number;

    /// <summary>
    /// Emote Usage
    /// </summary>
    public ClientFormat1D()
    {
        Encrypted = true;
        Command = 0x1D;
    }

    public override void Serialize(NetworkPacketReader reader) => Number = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}