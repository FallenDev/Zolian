using Darkages.Interfaces;

namespace Darkages.Network.Formats
{
    public abstract class NetworkFormat : INetworkFormat
    {
        public byte Command { get; set; }
        public bool Encrypted { get; set; }
        public abstract void Serialize(NetworkPacketReader reader);

        public abstract void Serialize(NetworkPacketWriter writer);
    }
}