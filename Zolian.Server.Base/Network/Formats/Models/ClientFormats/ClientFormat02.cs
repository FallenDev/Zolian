namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat02 : NetworkFormat
{
    public string AislingPassword;
    public string AislingUsername;

    /// <summary>
    /// Create Character Request
    /// </summary>
    public ClientFormat02()
    {
        Encrypted = true;
        OpCode = 0x02;
    }

    public override void Serialize(NetworkPacketReader reader)
    {
        AislingUsername = reader.ReadStringA();
        AislingPassword = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}