namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat58 : NetworkFormat
{
    /// <summary>
    /// Map Load Complete
    /// </summary>
    public ServerFormat58()
    {
        OpCode = 0x58;
        Encrypted = true;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) => writer.Write((ushort)0);
}