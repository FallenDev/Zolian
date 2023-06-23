using Darkages.Enums;

namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat79 : NetworkFormat
{
    /// <summary>
    /// Player Social Status
    /// </summary>
    public ClientFormat79()
    {
        Encrypted = true;
        OpCode = 0x79;
    }

    public ActivityStatus Status { get; private set; }

    public override void Serialize(NetworkPacketReader reader) => Status = (ActivityStatus) reader.ReadByte();

    public override void Serialize(NetworkPacketWriter writer) { }
}