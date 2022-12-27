namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat32 : NetworkFormat
{
    private readonly bool _doorPacket;
    private readonly byte _x;
    private readonly byte _y;
    private readonly bool _state;
    private readonly bool _openClosed;

    /// <summary>
    /// User Move Complete
    /// </summary>
    public ServerFormat32()
    {
        Encrypted = true;
        Command = 0x32;
    }

    /// <summary>
    /// Door Open/Closed
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="state"></param>
    /// <param name="openClosed"></param>
    /// <param name="door"></param>
    public ServerFormat32(byte x, byte y, bool state, bool openClosed, bool door) : this()
    {
        _x = x;
        _y = y;
        _state = state;
        _openClosed = openClosed;
        _doorPacket = door;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (_doorPacket)
        {
            writer.Write(byte.MinValue);
            writer.Write(_x);
            writer.Write(_y);
            writer.Write(_state);
            writer.Write(_openClosed);
            return;
        }

        writer.Write((byte)0x00);
    }
}