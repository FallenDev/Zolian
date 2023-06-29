namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat06 : NetworkFormat
{
    /// <summary>
    /// Map Edit
    /// </summary>
    public ServerFormat06()
    {
        Encrypted = true;
        OpCode = 0x06;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}