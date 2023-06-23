using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat63 : NetworkFormat
{
    /// <summary>
    /// Group Request
    /// </summary>
    public ServerFormat63()
    {
        Encrypted = true;
        OpCode = 0x63;
    }

    private string Username { get; }

    public ServerFormat63(Aisling aisling)
    {
        Username = aisling.Username;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write((byte)0x01);
        writer.WriteStringA(Username);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
    }
}