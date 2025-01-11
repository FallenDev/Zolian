using Darkages.Compression;
using Darkages.IO;
using Darkages.Models;

using ServiceStack;

namespace Darkages.Meta;

public class Metafile : CompressableObject
{
    public uint Hash { get; set; }
    public string Name { get; set; }
    public List<MetafileNode> Nodes { get; init; } = [];

    protected override void Load(MemoryStream stream)
    {
        using (var reader = new BufferReader(stream))
        {
            int length = reader.ReadUInt16();

            for (var i = 0; i < length; i++)
            {
                var node = new MetafileNode(reader.ReadStringA());
                var atomSize = reader.ReadUInt16();

                for (var j = 0; j < atomSize; j++)
                    node.Atoms.Add(reader.ReadStringB());

                Nodes.Add(node);
            }
        }

        Name = Path.GetFileName(Filename);
    }

    public override Stream Save(MemoryStream stream)
    {
        using var writer = new BufferWriter(stream);
        writer.Write((ushort)Nodes.Count);

        foreach (var node in Nodes)
        {
            writer.WriteStringA(node.Name);
            writer.Write((ushort)node.Atoms.Count);

            foreach (var atom in node.Atoms)
                writer.WriteStringB(atom);
        }

        return new MemoryStream(writer.BaseStream.ReadFully());
    }
}