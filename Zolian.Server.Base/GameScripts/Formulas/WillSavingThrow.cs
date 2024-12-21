using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Formulas;

[Script("Will Saving Throw")]
public class WillSavingThrow : FormulaScript
{
    public WillSavingThrow(Sprite obj) { }

    public override long Calculate(Sprite obj, long value)
    {
        var armor = obj.Will;
        var dmgMitigation = armor / 100f;

        if (obj.Will < 0)
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
                MonsterArmorType.Caster when dmgMitigation >= 0.98f => 0.98f,
                MonsterArmorType.Common when dmgMitigation >= 0.75f => 0.75f,
                MonsterArmorType.Tank when dmgMitigation >= 0.50f => 0.50f,
                _ => dmgMitigation
            };
        }
        else
        {
            if (dmgMitigation >= 0.85f)
                dmgMitigation = 0.85f;
        }
        
        var dmgReducedByMitigation = dmgMitigation * value;
        value -= (int)dmgReducedByMitigation;

        if (value <= 0)
            value = 1;

        return value;
    }
}