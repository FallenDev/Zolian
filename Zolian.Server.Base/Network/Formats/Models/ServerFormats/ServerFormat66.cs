namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat66 : NetworkFormat
{
    private const byte Type = 0x03;

    /// <summary>
    /// Nexon Verification Website
    /// </summary>
    public ServerFormat66()
    {
        Encrypted = true;
        Command = 0x66;
    }

    private static string Text => "https://classicrpgcharacter.nexon.com/service/ConfirmGameUser.aspx?id=%s&pw=%s&mainCode=2&subCode=0";

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Type);
        writer.WriteStringA(Text);
    }
}