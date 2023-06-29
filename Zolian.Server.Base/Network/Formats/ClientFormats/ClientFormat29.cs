namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat29 : NetworkFormat
{
    public uint ID;
    public byte ItemSlot;

    /// <summary>
    /// Item Dropped on Monster
    /// </summary>
    public ClientFormat29()
    {
        Encrypted = true;
        OpCode = 0x29;
    }

    public override void Serialize(NetworkPacketReader reader)
    {
        ItemSlot = reader.ReadByte();
        ID = reader.ReadUInt32();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}