namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat1A : NetworkFormat
{
    public byte Number;
    public int Serial;
    public short Speed;

    /// <summary>
    /// Player Animation
    /// </summary>
    /// <param name="serial"></param>
    /// <param name="number"></param>
    /// <param name="speed"></param>
    public ServerFormat1A(int serial, byte number, short speed) : this()
    {
        Serial = serial;
        Number = number;
        Speed = speed;
    }

    public ServerFormat1A()
    {
        Encrypted = true;
        Command = 0x1A;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Serial);
        writer.Write(Number);
        writer.Write(Speed);
        writer.Write(byte.MaxValue);
    }
}