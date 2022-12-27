namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat20 : NetworkFormat
{
    public byte Shade;
    private const byte Unknown = 0x01;

    /// <summary>
    /// Change Hour (Night - Time)
    /// Darkest = 0,
    /// Darker = 1,
    /// Dark = 2,
    /// Light = 3,
    /// Lighter = 4,
    /// Lightest = 5
    /// </summary>
    public ServerFormat20()
    {
        Encrypted = true;
        Command = 0x20;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Shade);
        writer.Write(Unknown);
    }
}