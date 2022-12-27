namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat43 : NetworkFormat
{
    public int Serial;
    public byte Type;

    /// <summary>
    /// Client Click
    /// </summary>
    public ClientFormat43()
    {
        Encrypted = true;
        Command = 0x43;
    }

    public short X { get; private set; }
    public short Y { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Type = reader.ReadByte();

        switch (Type)
        {
            case 0x01:
                Serial = reader.ReadInt32();
                break;
            case 0x02:
                break;
            case 0x03:
                X = reader.ReadInt16();
                Y = reader.ReadInt16();
                break;
        }
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}