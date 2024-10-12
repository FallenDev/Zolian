using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.ScriptingBase;

public abstract class DamageFormulaScript
{
    public abstract double Calculate(Sprite obj, Sprite target, MonsterEnums type);
}