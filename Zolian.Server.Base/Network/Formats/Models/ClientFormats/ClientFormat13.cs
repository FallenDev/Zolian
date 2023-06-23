namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat13 : NetworkFormat
{
    /// <summary>
    /// SpaceBar -Assail Action-
    /// </summary>
    public ClientFormat13()
    {
        Encrypted = true;
        OpCode = 0x13;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}