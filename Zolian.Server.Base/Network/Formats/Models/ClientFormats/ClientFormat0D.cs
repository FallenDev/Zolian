namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat0D : NetworkFormat
{
    /// <summary>
    /// Ignore Player
    /// </summary>
    private enum Ignore : byte
    {
        Request = 1,
        Add = 2,
        Remove = 3,
    }

    public string Target { get; set; }
    public byte IgnoreType { get; set; }

    public ClientFormat0D()
    {
        Encrypted = true;
        OpCode = 0x0D;
    }

    public override void Serialize(NetworkPacketReader reader)
    {
        var ignoreType = (Ignore)reader.ReadByte();
        IgnoreType = (byte)ignoreType;

        if (ignoreType != Ignore.Request)
            Target = reader.ReadStringA();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}