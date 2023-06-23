namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat44 : NetworkFormat
{
    public byte Slot;

    /// <summary>
    /// Unequip Item
    /// </summary>
    public ClientFormat44()
    {
        Encrypted = true;
        OpCode = 0x44;
    }

    public override void Serialize(NetworkPacketReader reader) => Slot = reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}