namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat10 : NetworkFormat
{
    /// <summary>
    /// Remove from Inventory
    /// </summary>
    /// <param name="slot"></param>
    public ServerFormat10(byte slot) : this() => Slot = slot;

    private ServerFormat10()
    {
        Encrypted = true;
        Command = 0x10;
    }

    private byte Slot { get; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) => writer.Write(Slot);
}