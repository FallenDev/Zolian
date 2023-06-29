namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat08 : NetworkFormat
{
    /// <summary>
    /// Item Drop
    /// </summary>
    public ClientFormat08()
    {
        Encrypted = true;
        OpCode = 0x08;
    }

    public int ItemAmount { get; private set; }
    public byte ItemSlot { get; private set; }
    private short Unknown { get; set; }
    public short X { get; private set; }
    public short Y { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        ItemSlot = reader.ReadByte();
        X = reader.ReadInt16();
        Y = reader.ReadInt16();
        ItemAmount = reader.ReadInt32();

        if (reader.GetCanRead())
            Unknown = reader.ReadInt16();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}