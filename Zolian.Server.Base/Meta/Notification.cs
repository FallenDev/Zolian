using Darkages.Common;
using Darkages.Compression;

namespace Darkages.Meta;

public class Notification : CompressableObject
{
    public byte[] Data => DeflatedData;

    public uint Hash { get; private set; }
    public ushort Size => (ushort)DeflatedData.Length;

    public static Notification FromFile(string filename)
    {
        var result = new Notification();
        var message = File.ReadAllText(filename);

        result.InflatedData = message.ToByteArray();
        result.Hash = Crc32Provider.Generate32(result.InflatedData);
        result.Compress();

        return result;
    }
}