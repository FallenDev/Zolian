namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat04 : NetworkFormat
{
    /// <summary>
    /// Character Creation Finalization
    /// </summary>
    public ClientFormat04()
    {
        Encrypted = true;
        OpCode = 0x04;
    }

    public byte Gender { get; private set; }
    public byte HairColor { get; private set; }
    public byte HairStyle { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        HairStyle = reader.ReadByte();
        Gender = reader.ReadByte();
        HairColor = reader.ReadByte();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}