using System.Security.Cryptography;
using System.Text;
using Darkages.Common;
using Darkages.Interfaces;

namespace Darkages.Network.Security;

[Serializable]
public sealed class SecurityParameters : IFormattableNetwork
{
    public static readonly SecurityParameters Default = new(0, Encoding.ASCII.GetBytes("NexonInc."));

    public SecurityParameters()
    {
        Seed = (byte) RandomNumberGenerator.GetInt32(10);
        Salt = Generator.GenerateString(9).ToByteArray();
    }

    public SecurityParameters(byte seed, byte[] key)
    {
        Seed = seed;
        Salt = key;
    }

    public byte[] Salt { get; set; }
    public byte Seed { get; set; }

    public void Serialize(NetworkPacketReader reader)
    {
        Seed = reader.ReadByte();
        Salt = reader.ReadBytes(reader.ReadByte());
    }

    public void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Seed);
        writer.Write((byte) Salt.Length);
        writer.Write(Salt);
    }
}