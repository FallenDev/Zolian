namespace Darkages.Models;

public sealed class MetafileNode
{
    private static readonly Dictionary<string, string> StringPool = new(StringComparer.Ordinal);
    private static readonly Lock StringPoolSync = new();

    public MetafileNode(string name)
    {
        Name = Intern(name);
        Atoms = [];
    }

    public MetafileNode(string name, string atom)
    {
        Name = Intern(name);
        Atoms = [Intern(atom)];
    }

    public MetafileNode(string name, params string[] atoms)
    {
        Name = Intern(name);

        if (atoms == null || atoms.Length == 0)
        {
            Atoms = [];
            return;
        }

        for (var i = 0; i < atoms.Length; i++)
            atoms[i] = Intern(atoms[i]);

        Atoms = atoms;
    }

    public string[] Atoms { get; }
    public string Name { get; }

    private static string Intern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        lock (StringPoolSync)
        {
            if (StringPool.TryGetValue(value, out var pooled))
                return pooled;

            StringPool[value] = value;
            return value;
        }
    }
}