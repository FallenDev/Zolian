namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat73 : NetworkFormat
{
    /// <summary>
    /// Unknown
    /// </summary>
    public ServerFormat73()
    {
        Encrypted = true;
        Command = 0x73;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) => writer.Write(byte.MinValue);
}