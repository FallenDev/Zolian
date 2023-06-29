#region

using Darkages.Enums;

#endregion

namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat30 : NetworkFormat
{
    public byte MovingFrom;
    public byte MovingTo;
    public Pane PaneType;

    /// <summary>
    /// Swap Slot
    /// </summary>
    public ClientFormat30()
    {
        Encrypted = true;
        OpCode = 0x30;
    }

    public override void Serialize(NetworkPacketReader reader)
    {
        PaneType = (Pane) reader.ReadByte();
        MovingFrom = reader.ReadByte();
        MovingTo = reader.ReadByte();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}