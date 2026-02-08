using Darkages.Sprites;

namespace Darkages.ScriptingBase;

public abstract class FormulaScript
{
    public abstract long Calculate(Sprite obj, long value, double penetration);
}