namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat03 : NetworkFormat
{
    /// <summary>
    /// Login
    /// </summary>
    public ClientFormat03()
    {
        Encrypted = true;
        Command = 0x03;
    }

    public string Password { get; private set; }
    public string Username { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Username = reader.ReadStringA();
        Password = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}