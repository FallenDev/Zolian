using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

[Script("AC Formula")]
public class Ac() : FormulaScript
{
    public override long Calculate(Sprite obj, long value)
    {
        var armor = obj.SealedAc;
        var dmgMitigation = armor / 100f;

        if (obj.SealedAc < 0)
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

        if (obj is Monster monster)
        {
            dmgMitigation = monster.Template.MonsterArmorType switch
            {
                MonsterArmorType.Caster when dmgMitigation >= 0.90f => 0.90f,
                MonsterArmorType.Common when dmgMitigation >= 0.95f => 0.95f,
                MonsterArmorType.Tank when dmgMitigation >= 0.98f => 0.98f,
                _ => dmgMitigation
            };
        }
        else
        {
            if (dmgMitigation >= 0.98f)
                dmgMitigation = 0.98f;
        }

        var dmgReducedByMitigation = dmgMitigation * value;
        value -= (int)dmgReducedByMitigation;

        if (value <= 0)
            value = 1;

        return value;
    }
}