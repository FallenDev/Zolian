namespace Darkages.Interfaces;

public interface IFormattableNetwork
{
    void Serialize(NetworkPacketReader reader);
    void Serialize(NetworkPacketWriter writer);
}