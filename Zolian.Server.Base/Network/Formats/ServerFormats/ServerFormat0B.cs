namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat0B : NetworkFormat
{
    private byte Direction { get; }
    private short X { get; }
    private short Y { get; }

    /// <summary>
    /// Aisling Move
    /// </summary>
    public ServerFormat0B(byte dir, short x, short y) : this()
    {
        Direction = dir;
        X = x;
        Y = y;
    }

    private ServerFormat0B()
    {
        Encrypted = true;
        OpCode = 0x0B;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Direction);
        writer.Write(X);
        writer.Write(Y);
        writer.Write((short)0x0B);
        writer.Write((short)0x0B);
        writer.Write((byte)0x01);
    }
}