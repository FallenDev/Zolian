namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat26 : NetworkFormat
{
    /// <summary>
    /// Password Change
    /// </summary>
    public ClientFormat26()
    {
        Encrypted = true;
        Command = 0x26;
    }

    public string NewPassword { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Username = reader.ReadStringA();
        Password = reader.ReadStringA();
        NewPassword = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}