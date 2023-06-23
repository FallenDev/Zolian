using Darkages.Network;

namespace Darkages.Interfaces;

public interface INetworkFormat
{
    byte OpCode { get; set; }
    bool Encrypted { get; set; }
    void Serialize(NetworkPacketReader reader);
    void Serialize(NetworkPacketWriter writer);
}