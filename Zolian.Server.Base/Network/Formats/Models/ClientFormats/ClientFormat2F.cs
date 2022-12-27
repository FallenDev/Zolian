namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat2F : NetworkFormat
{
    /// <summary>
    /// Toggle Group
    /// </summary>
    public ClientFormat2F()
    {
        Encrypted = true;
        Command = 0x2F;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}