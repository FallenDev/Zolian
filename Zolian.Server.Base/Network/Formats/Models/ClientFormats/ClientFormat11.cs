namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat11 : NetworkFormat
{
    /// <summary>
    /// Change Direction
    /// </summary>
    public ClientFormat11()
    {
        Encrypted = true;
        Command = 0x11;
    }

    public byte Direction { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Direction = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}