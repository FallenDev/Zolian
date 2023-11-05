using Chaos.IO.Memory;
using Chaos.Packets;
using Chaos.Packets.Abstractions.Definitions;

using System.Text;

namespace Darkages.Network.Client;

public static class ServerPacketEx
{
    public static ServerPacket FromData(ServerOpCode opCode, Encoding encoding, params byte[] data)
    {
        var packet = new ServerPacket(opCode);
        if (data.Length <= 0) return packet;
        var writer = new SpanWriter(encoding, data.Length);
        writer.WriteBytes(data);
        packet.Buffer = writer.ToSpan();
        return packet;
    }
}