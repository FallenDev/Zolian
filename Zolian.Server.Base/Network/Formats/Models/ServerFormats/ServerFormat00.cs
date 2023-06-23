using Darkages.Network.Security;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat00 : NetworkFormat
{
    /// <summary>
    /// CryptoKey
    /// </summary>
    public ServerFormat00()
    {
        Encrypted = false;
        OpCode = 0x00;
    }

    public uint Hash { get; init; }
    public SecurityProvider Parameters { get; init; }
    public byte Type { get; init; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Parameters.Salt);
        writer.Write(Parameters.Seed);
        writer.Write(Parameters);
    }
}