using Chaos.Cryptography;

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
                var name = reader.ReadStringA();
                var atomSize = reader.ReadUInt16();

                if (atomSize == 0)
                {
                    Nodes.Add(new MetafileNode(name));
                    continue;
                }

                var atoms = new string[atomSize];
                for (var j = 0; j < atomSize; j++)
                    atoms[j] = reader.ReadStringB();

                Nodes.Add(new MetafileNode(name, atoms));
            }
        }

        Hash = Crc.Generate32(InflatedData);
        Name = Path.GetFileName(Filename);
    }

    public override Stream Save(MemoryStream stream)
    {
        using var writer = new BufferWriter(stream);
        writer.Write((ushort)Nodes.Count);

        foreach (var node in Nodes)
        {
            writer.WriteStringA(node.Name);
            writer.Write((ushort)node.Atoms.Length);

            foreach (var atom in node.Atoms)
                writer.WriteStringB(atom);
        }

        return new MemoryStream(writer.BaseStream.ReadFully());
    }
}