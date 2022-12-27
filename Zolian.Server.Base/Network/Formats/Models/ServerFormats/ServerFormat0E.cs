namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat0E : NetworkFormat
{
    private readonly int _serial;

    /// <summary>
    /// Remove World Object
    /// </summary>
    /// <param name="serial"></param>
    public ServerFormat0E(int serial) : this() => _serial = serial;

    private ServerFormat0E()
    {
        Encrypted = true;
        Command = 0x0E;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) => writer.Write(_serial);
}