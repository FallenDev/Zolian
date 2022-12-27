using Darkages.Interfaces;
using Darkages.Network;

namespace Darkages.Meta;

public class MetafileCollection : List<Metafile>, IFormattableNetwork
{
    public MetafileCollection(int capacity) : base(capacity) { }

    public void Serialize(NetworkPacketReader reader) { }

    public void Serialize(NetworkPacketWriter writer)
    {
        writer.Write((ushort)Count);

        foreach (var metafile in this)
        {
            writer.WriteStringA(metafile.Name);
            writer.Write(metafile.Hash);
        }
    }
}