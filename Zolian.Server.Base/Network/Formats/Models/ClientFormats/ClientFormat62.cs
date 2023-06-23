namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat62 : NetworkFormat
{
    /// <summary>
    /// Sequence Change
    /// </summary>
    public ClientFormat62()
    {
        Encrypted = false;
        OpCode = 0x62;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}