namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat38 : NetworkFormat
{
    /// <summary>
    /// Request Refresh Client
    /// </summary>
    public ClientFormat38()
    {
        Encrypted = true;
        OpCode = 0x38;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}