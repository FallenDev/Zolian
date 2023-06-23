namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat3B : NetworkFormat
{
    public string To, Title, Message;

    /// <summary>
    /// Request Bulletin Board 
    /// </summary>
    public ClientFormat3B()
    {
        Encrypted = true;
        OpCode = 0x3B;
    }

    public ushort BoardIndex { get; private set; }
    public ushort TopicIndex { get; private set; }
    public byte Type { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        if (!reader.GetCanRead()) return;

        Type = reader.ReadByte();
        if (reader.GetCanRead()) BoardIndex = reader.ReadUInt16();
        if (reader.GetCanRead()) TopicIndex = reader.ReadUInt16();

        switch (Type)
        {
            case 0x06:
                reader.Position = 0;
                reader.ReadByte();
                BoardIndex = reader.ReadUInt16();

                To = reader.ReadStringA();
                Title = reader.ReadStringA();
                Message = reader.ReadStringB();
                break;
            case 0x04:
                reader.Position = 0;
                reader.ReadByte();
                BoardIndex = reader.ReadUInt16();

                Title = reader.ReadStringA();
                Message = reader.ReadStringB();
                break;
        }
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}