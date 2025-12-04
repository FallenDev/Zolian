using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Formulas;

[Script("AC Formula")]
public class Ac : FormulaScript
{
    public Ac(Sprite obj) { }

    public override long Calculate(Sprite obj, long value)
    {
        if (value <= 0)
            return 1;

        // Armor is clamped to [-200, 500] in Ac Property
        // SealedAc only reduces that range
        var armor = obj.SealedAc;

        // Sanitize armor value
        if (armor < -200)
            armor = -200;

        // -----------------------------------------------------
        // NEGATIVE AC → takes MORE damage, bounded (no Dmg use)
        // -----------------------------------------------------
        if (armor < 0)
        {
            // Every point below 0 AC is +1% damage taken.
            // -100 AC => +100% (2x total)
            // -200 AC => +200% (3x total)
            var penalty = Math.Min(System.Math.Abs(armor) / 100.0, 2.0); // 0..2
            var multiplier = 1.0 + penalty;                              // 1.0..3.0

            var increased = (long)(value * multiplier);
            return increased <= 0 ? 1 : increased;
        }

        // No armor = no mitigation
        if (armor == 0)
            return value;

        // -----------------------------------------------------
        // POSITIVE AC → diminishing returns
        // Uses role-dependent caps for monsters
        // Uses 90% cap for players
        // -----------------------------------------------------
        double mitigationCurve;
        double maxCap;

        if (obj is Monster monster)
        {
            // DR-style, role-based:
            (mitigationCurve, maxCap) = monster.Template.MonsterArmorType switch
            {
                // Tanks: strong vs physical (70% cap, fast curve)
                MonsterArmorType.Tank => (120.0, 0.70),

                // Common: balanced (55% cap, medium curve)
                MonsterArmorType.Common => (140.0, 0.55),

                // Casters: weak vs physical (40% cap, slow curve)
                MonsterArmorType.Caster => (160.0, 0.40),
                _ => (140.0, 0.55)
            };
        }
        else
        {
            // -----------------------------------------
            // PLAYER AC MODEL
            // -----------------------------------------
            mitigationCurve = 75.0;
            maxCap = 0.90;  // 90% max physical mitigation
        }

        var mitigation = armor / (armor + mitigationCurve);
        if (mitigation < 0.0)
            mitigation = 0.0;
        if (mitigation > maxCap)
            mitigation = maxCap;

        var reduced = (long)(value * mitigation);
        var result = value - reduced;

        return result <= 0 ? 1 : result;
    }
}