using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

[Script("AC Formula")]
public class Ac : FormulaScript
{
    private readonly Sprite _obj;

    public Ac(Sprite obj)
    {
        _obj = obj;
    }

    public override long Calculate(Sprite obj, long value)
    {
        var armor = obj.Ac;
        var dmgMitigation = armor / 100f;

        if (obj.Ac < 0)
        {
            var dmgIncreasedByMitigation = Math.Abs(dmgMitigation) * value;
            value += (int)dmgIncreasedByMitigation;

            if (value <= 0)
                value = 1;

            if (obj.Dmg <= 0) return value;

            var dmgModifier = obj.Dmg * 0.25;
            dmgModifier /= 100;
            var dmgBoost = dmgModifier * value;
            value += (int)dmgBoost;

            if (value <= 0)
                value = 1;

            return value;
        }

        if (dmgMitigation >= 0.98f)
            dmgMitigation = 0.98f;

        var dmgReducedByMitigation = dmgMitigation * value;
        value -= (int)dmgReducedByMitigation;

        if (value <= 0)
            value = 1;

        var dmgAboveAcModifier = obj.Str * 0.25;
        dmgAboveAcModifier /= 100;
        var dmgAboveAcBoost = dmgAboveAcModifier * value;
        value += (int)dmgAboveAcBoost;

        if (value <= 0)
            value = 1;

        return value;
    }
}