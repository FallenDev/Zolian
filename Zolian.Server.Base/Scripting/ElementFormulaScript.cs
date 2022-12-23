using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Scripting
{
    public abstract class ElementFormulaScript : IScriptBase
    {
        public abstract double Calculate(Sprite obj, ElementManager.Element offenseElement);
        public abstract double FasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement);
        public abstract double MorFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement);
        public abstract double ArdFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement);
    }
}
