namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat48 : NetworkFormat
{
    /// <summary>
    /// Cancel Casting
    /// </summary>
    public ServerFormat48()
    {
        Encrypted = true;
        OpCode = 0x48;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) => writer.Write(byte.MinValue);
}