using Darkages.Network.Security;

namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat10 : NetworkFormat
{
    /// <summary>
    /// Client Redirected
    /// </summary>
    public ClientFormat10()
    {
        Encrypted = false;
        Command = 0x10;
    }

    public string Name { get; private set; }
    public SecurityParameters Parameters { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Parameters = reader.ReadObject<SecurityParameters>();
        Name = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}