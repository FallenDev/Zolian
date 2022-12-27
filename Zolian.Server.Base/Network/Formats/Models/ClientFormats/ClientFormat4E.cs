namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat4E : NetworkFormat
{
    /// <summary>
    /// Chant Message
    /// </summary>
    public ClientFormat4E()
    {
        Encrypted = true;
        Command = 0x4E;
    }

    public string Message { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Message = reader.ReadStringA();

    public override void Serialize(NetworkPacketWriter writer) { }
}