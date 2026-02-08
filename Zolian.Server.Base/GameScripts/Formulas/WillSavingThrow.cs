using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Formulas;

[Script("Will Saving Throw")]
public class WillSavingThrow() : FormulaScript
{
    /// <summary>
    /// Calculates the mitigated value after applying the target's Will and a specified magic penetration factor.
    /// </summary>
    /// <remarks>Mitigation is calculated using a diminishing returns formula based on the target's Will
    /// attribute. For monsters, the mitigation curve and maximum cap depend on their armor type; for players, a
    /// standard curve and cap are used. Magic penetration reduces the effective Will before mitigation is applied, but
    /// only affects positive Will values. The result is always at least 1.</remarks>
    /// <param name="obj">The target <see cref="Sprite"/> whose Will attribute is used to determine mitigation. Must not be null.</param>
    /// <param name="value">The initial value to be mitigated. Must be greater than zero to apply mitigation; otherwise, the method returns 1.</param>
    /// <param name="penetration">The percentage of magic penetration to apply, as a value between 0.0 and 0.50. Values outside this range are clamped.</param>
    /// <returns>The resulting damage value after accounting for armor mitigation or amplification. Returns at least 1.</returns>
    public override long Calculate(Sprite obj, long value, double penetration)
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

        // -----------------------------------------------------
        // Apply magic penetration
        // -----------------------------------------------------
        if (penetration > 0.0)
        {
            // Clamp penetration to [0..0.50]
            if (penetration > 0.50)
                penetration = 0.50;

            mitigation *= (1.0 - penetration);

            if (mitigation < 0.0)
                mitigation = 0.0;
        }

        var reduced = (long)(value * mitigation);
        var result = value - reduced;

        return result <= 0 ? 1 : result;
    }
}