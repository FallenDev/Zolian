using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.ScriptingBase;

public abstract class ElementFormulaScript
{
    public abstract double Calculate(Sprite defender, Sprite attacker, ElementManager.Element offenseElement);
    public abstract double FasNadur(ElementManager.Element offenseElement, ElementManager.Element defenseElement);
    public abstract double MorFasNadur(ElementManager.Element offenseElement, ElementManager.Element defenseElement);
    public abstract double ArdFasNadur(ElementManager.Element offenseElement, ElementManager.Element defenseElement);
}