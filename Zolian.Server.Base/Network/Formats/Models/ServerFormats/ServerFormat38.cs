namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat38 : NetworkFormat
{
    /// <summary>
    /// Remove Equipment
    /// </summary>
    /// <param name="slot"></param>
    public ServerFormat38(byte slot) : this() => Slot = slot;

    private ServerFormat38()
    {
        Encrypted = true;
        Command = 0x38;
    }

    private byte Slot { get; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter packet) => packet.Write(Slot);
}