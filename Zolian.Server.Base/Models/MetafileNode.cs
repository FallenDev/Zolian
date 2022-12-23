using System.Collections.Specialized;

namespace Darkages.Models
{
    public class MetafileNode
    {
        public MetafileNode(string name, params string[] atoms)
        {
            Name = name;
            Atoms = new StringCollection();
            Atoms.AddRange(atoms);
        }

        public StringCollection Atoms { get; }
        public string Name { get; }
    }
}