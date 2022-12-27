namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat3A : NetworkFormat
{
    /// <summary>
    /// Mundane Input Response
    /// </summary>
    public ClientFormat3A()
    {
        Encrypted = true;
        Command = 0x3A;
    }

    public string Input { get; private set; }
    public ushort ScriptId { get; private set; }
    public uint Serial { get; private set; }
    public ushort Step { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        reader.ReadByte();
        var id = reader.ReadUInt32();
        var scriptId = reader.ReadUInt16();
        var step = reader.ReadUInt16();

        if (reader.ReadByte() == 0x02)
            if (reader.GetCanRead())
                Input = reader.ReadStringA();

        ScriptId = scriptId;
        Step = step;
        Serial = id;
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}