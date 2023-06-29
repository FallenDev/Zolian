namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat3B : NetworkFormat
{
    /// <summary>
    /// Server Heartbeat Send
    /// </summary>
    public ServerFormat3B()
    {
        Encrypted = true;
        OpCode = 0x3B;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(ServerSetup.Instance.EncryptKeyConDict.Values.FirstOrDefault()); // first
        writer.Write((byte)0x14); // second
    }
}