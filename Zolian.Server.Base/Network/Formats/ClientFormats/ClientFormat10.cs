namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat10 : NetworkFormat
{
    /// <summary>
    /// Client Redirected
    /// </summary>
    public ClientFormat10()
    {
        Encrypted = false;
        OpCode = 0x10;
    }

    public string Name { get; private set; }
    public SecurityProvider Parameters { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Parameters = reader.ReadObject<SecurityProvider>();
        Name = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}