using Darkages.Types;

namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat07 : NetworkFormat
{
    /// <summary>
    /// Item Pickup
    /// </summary>
    public ClientFormat07()
    {
        Encrypted = true;
        OpCode = 0x07;
    }

    private byte PickupType { get; set; }
    public Position Position { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        PickupType = reader.ReadByte();
        Position = reader.ReadPosition();
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}