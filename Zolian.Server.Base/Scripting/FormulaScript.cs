using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Scripting
{
    public abstract class FormulaScript : IScriptBase
    {
        public abstract long Calculate(Sprite obj, long value);
    }
}
