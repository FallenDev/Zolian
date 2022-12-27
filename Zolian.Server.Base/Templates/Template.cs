using Darkages.Object;

namespace Darkages.Templates;

public abstract class Template : ObjectManager
{
    public string Description { get; set; }
    public string Group { get; init; }
    public string Name { get; set; }
    public string DamageMod { get; set; }
}