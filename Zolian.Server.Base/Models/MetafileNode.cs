namespace Darkages.Models;

public class MetafileNode
{
    public MetafileNode(string name, params string[] atoms)
    {
        Name = name;
        Atoms = atoms ?? [];
    }

    public string[] Atoms { get; }
    public string Name { get; }
}