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
        if (value <= 0)
            return 1;

        // Will is clamped to [0, 90] in Sprite
        var will = obj.Will;

        // Zero Will = no mitigation
        if (will <= 0)
            return value;

        // -----------------------------------------------------
        // POSITIVE WILL -> diminishing returns
        // Uses role-dependent caps for monsters
        // -----------------------------------------------------
        double mitigationCurve;
        double maxCap;

        if (obj is Monster monster)
        {
            // DR-style, role-based:
            (mitigationCurve, maxCap) = monster.Template.MonsterArmorType switch
            {
                // Casters: strong vs magic (90% cap, leans on Will more)
                MonsterArmorType.Caster => (40.0, 0.90),

                // Commons: balanced (75% cap, medium curve)
                MonsterArmorType.Common => (60.0, 0.75),

                // Tanks: weak vs magic (50% cap, leans less on Will)
                MonsterArmorType.Tank => (75.0, 0.50),
                _ => (60.0, 0.75)
            };
        }
        else
        {
            // -----------------------------------------
            // PLAYER Will MODEL
            // -----------------------------------------
            mitigationCurve = 60.0;
            maxCap = 0.85; // 85% max magical mitigation
        }

        var mitigation = will / (will + mitigationCurve);
        if (mitigation < 0.0)
            mitigation = 0.0;
        if (mitigation > maxCap)
            mitigation = maxCap;

        var reduced = (long)(value * mitigation);
        var result = value - reduced;

        return result <= 0 ? 1 : result;
    }
}