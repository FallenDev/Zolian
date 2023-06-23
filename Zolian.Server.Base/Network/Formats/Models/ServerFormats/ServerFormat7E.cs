namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat7E : NetworkFormat
{
    private const string Text = "CONNECTED SERVER\n";
    private const byte Type = 0x1B;

    /// <summary>
    /// TownMap -- Handshake
    /// </summary>
    public ServerFormat7E()
    {
        Encrypted = false;
        OpCode = 0x7E;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Type);
        writer.WriteString(Text);
    }
}