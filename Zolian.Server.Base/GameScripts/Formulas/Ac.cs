using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Formulas;

[Script("AC Formula")]
public class Ac() : FormulaScript
{
    /// <summary>
    /// Calculates the final damage value after applying the target's armor and a specified armor penetration factor.
    /// </summary>
    /// <remarks>Mitigation is calculated using a diminishing returns formula based on the target's Sealed ArmorClass. 
    /// Positive armor reduces damage with diminishing returns, subject to role-based or player-specific caps. Armor penetration
    /// reduces the effectiveness of positive armor only, up to a maximum of 50%. The method ensures that the returned
    /// damage is always at least 1.</remarks>
    /// <param name="obj">The target <see cref="Sprite"/> whose armor value is used to determine damage mitigation or amplification.</param>
    /// <param name="baseDamage">The base damage amount before armor and penetration are applied. Must be greater than 0.</param>
    /// <param name="penetration">The fraction of the target's positive armor to ignore, as a value between 0.0 and 0.5. Only applies to positive armor values.</param>
    /// <returns>The resulting damage value after accounting for armor mitigation or amplification. Returns at least 1.</returns>
    public override long Calculate(Sprite obj, long baseDamage, double penetration)
    {
        if (baseDamage <= 0)
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

            var increased = (long)(baseDamage * multiplier);
            return increased <= 0 ? 1 : increased;
        }

        // No armor = no mitigation
        if (armor == 0)
            return baseDamage;

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

        // -----------------------------------------------------
        // Apply armor penetration
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

        var reduced = (long)(baseDamage * mitigation);
        var result = baseDamage - reduced;

        return result <= 0 ? 1 : result;
    }
}