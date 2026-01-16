using System.Runtime.CompilerServices;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Creations;

public static class MonsterMath
{
    // ----------------------------
    // Precomputed vitality table
    // ----------------------------
    private const int VitalityBaseLevel = 250;
    private const int VitalityMaxLevel = 1200;
    private const int VitalityStep = 5;

    // Index 0 => level 250
    // Index n => level 250 + n*5
    private static readonly (long Hp, long Mp)[] s_vitality250Plus = BuildVitality250Plus();

    // ----------------------------
    // Precomputed experience table
    // ----------------------------
    private const int ExperienceSeedLevel = 200;
    private const int ExperienceBaseLevel = 205;
    private const int ExperienceMaxLevel = 1200;
    private const int ExperienceStep = 5;

    // Index 0 => level 205
    // Index n => level 205 + n*5
    private static readonly (long Start, long End)[] s_experience205Plus = BuildExperience205Plus();

    // ----------------------------
    // Precomputed ability table
    // ----------------------------
    private const int AbilitySeedLevel = 500;
    private const int AbilityBaseLevel = 505;
    private const int AbilityMaxLevel = 1200;
    private const int AbilityStep = 5;

    // Index 0 => level 505
    // Index n => level 505 + n*5
    private static readonly (int Start, int End)[] s_ability505Plus = BuildAbility505Plus();

    /// <summary>
    /// Returns the first row where (level = row.Level). If none match, returns the last row.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (T1 A, T2 B) FirstLE<T1, T2>(ReadOnlySpan<(int Level, T1 A, T2 B)> table, int level)
    {
        foreach (var row in table)
            if (level <= row.Level)
                return (row.A, row.B);

        var last = table[^1];
        return (last.A, last.B);
    }

    /// <summary>
    /// Builds an array of hit point (HP) and mana point (MP) values for character vitality levels at 250 and above.
    /// </summary>
    /// <remarks>The returned array includes values for all vitality levels from 250 up to the maximum, in
    /// increments defined by the vitality step. The HP and MP values are calculated using a growth rate function for
    /// each level.</remarks>
    /// <returns>An array of tuples, where each tuple contains the HP and MP values for a corresponding vitality level starting
    /// at 250. The first element represents level 250, and each subsequent element corresponds to the next vitality
    /// milestone.</returns>
    private static (long Hp, long Mp)[] BuildVitality250Plus()
    {
        var count = ((VitalityMaxLevel - VitalityBaseLevel) / VitalityStep) + 1;
        var arr = new (long Hp, long Mp)[count];

        // Base at 250: must match your milestone table last entry
        long hp = 9150;
        long mp = 4000;

        arr[0] = (hp, mp);

        // Fill 255..1200 step 5
        for (var i = 1; i < arr.Length; i++)
        {
            var level = VitalityBaseLevel + (i * VitalityStep);
            var g = GetVitalityGrowthRate(level);

            hp = (long)Math.Round(hp * (1.0 + g));
            mp = (long)Math.Round(mp * (1.0 + g));

            arr[i] = (hp, mp);
        }

        return arr;
    }

    /// <summary>
    /// Builds an array of experience point ranges for levels above 205, applying a fixed percentage increase to each
    /// subsequent level.
    /// </summary>
    /// <remarks>The calculation uses the last milestone from the existing experience milestones as a seed and
    /// applies a 5% increase for each new level. This method is intended for generating experience requirements for
    /// high-level progression beyond predefined milestones.</remarks>
    /// <returns>An array of tuples, where each tuple contains the start and end experience values for each level above 205. The
    /// array is ordered by increasing level.</returns>
    private static (long Start, long End)[] BuildExperience205Plus()
    {
        var count = ((ExperienceMaxLevel - ExperienceBaseLevel) / ExperienceStep) + 1;
        var arr = new (long Start, long End)[count];

        // Seed from the last milestone entry (expected: level 200)
        var last = MonsterArrays.ExperienceMilestones[^1];
        long start = last.Start;
        long end = last.End;

        // First entry is 205 = seed * 1.05 once, matching your loop that starts at 205.
        start = (long)(start * 1.05);
        end = (long)(end * 1.05);
        arr[0] = (start, end);

        for (var i = 1; i < arr.Length; i++)
        {
            start = (long)(start * 1.05);
            end = (long)(end * 1.05);
            arr[i] = (start, end);
        }

        return arr;
    }

    /// <summary>
    /// Builds an array of ability milestone ranges for levels above 500, applying a 5 percent increase to each
    /// subsequent range.
    /// </summary>
    /// <remarks>The first milestone is calculated by applying a 5 percent increase to the last milestone
    /// entry for level 500. Each subsequent milestone applies an additional 5 percent increase to the previous values.
    /// The number of milestones generated is determined by the configured ability level range and step size.</remarks>
    /// <returns>An array of tuples, where each tuple contains the start and end values for an ability milestone range above
    /// level 500. The array is ordered by increasing level.</returns>
    private static (int Start, int End)[] BuildAbility505Plus()
    {
        var count = ((AbilityMaxLevel - AbilityBaseLevel) / AbilityStep) + 1;
        var arr = new (int Start, int End)[count];

        // Seed from the last milestone entry (expected: level 500)
        var last = MonsterArrays.AbilityMilestones[^1];
        int start = last.Start;
        int end = last.End;

        // First entry is 505 = seed * 1.05 once, matching your loop that starts at 505.
        start = (int)(start * 1.05);
        end = (int)(end * 1.05);
        arr[0] = (start, end);

        for (var i = 1; i < arr.Length; i++)
        {
            start = (int)(start * 1.05);
            end = (int)(end * 1.05);
            arr[i] = (start, end);
        }

        return arr;
    }

    /// <summary>
    /// Calculates the HP and MP vitality multipliers for a specified level.
    /// </summary>
    /// <remarks>The returned multipliers are determined based on milestone data for lower levels and a
    /// stepped progression for higher levels. If the specified level exceeds the maximum supported level, the
    /// multipliers for the maximum level are returned.</remarks>
    /// <param name="level">The level for which to retrieve vitality multipliers. Must be greater than or equal to zero.</param>
    /// <returns>A tuple containing the HP multiplier and MP multiplier for the specified level.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (long HpMultiplier, long MpMultiplier) GetVitalityMultipliers(int level)
    {
        if (level <= VitalityBaseLevel)
        {
            var (hp, mp) = FirstLE(MonsterArrays.VitalityMilestones, level);
            return (hp, mp);
        }

        var target = RoundUpToStep(level, VitalityStep);
        if (target > VitalityMaxLevel) target = VitalityMaxLevel;

        var idx = (target - VitalityBaseLevel) / VitalityStep;
        var (hp2, mp2) = s_vitality250Plus[idx];
        return (hp2, mp2);
    }

    /// <summary>
    /// Calculates the vitality growth rate based on the specified level.
    /// </summary>
    /// <remarks>The growth rate decreases as the level increases, with specific thresholds at levels 500,
    /// 750, and 900.</remarks>
    /// <param name="level">The level for which to determine the vitality growth rate. Must be a non-negative integer.</param>
    /// <returns>A double value representing the vitality growth rate for the given level.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetVitalityGrowthRate(int level)
    {
        if (level <= 500) return 0.05;
        if (level <= 750) return 0.03;
        if (level <= 900) return 0.02;
        return 0.015;
    }

    /// <summary>
    /// Applies high-level vitality scaling to the specified monster, increasing its base and bonus hit points for
    /// levels 750 and above.
    /// </summary>
    /// <remarks>This method has no effect if the monster's level is below 750. For higher levels, the
    /// monster's base and bonus hit points are increased according to a scaling curve. This adjustment is intended for
    /// balancing high-level monsters.</remarks>
    /// <param name="obj">The monster whose vitality (base and bonus hit points) will be scaled based on its level. Must not be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyHighLevelVitalityScaling(Monster obj)
    {
        var level = obj.Template.Level;
        if (level < 750) return;

        // Tune knobs
        const double hpScaleAt750 = 1.00;
        const double hpScaleAt1000 = 6.00;
        const double hpScaleAt1200 = 6.50;

        var hpScale = GetBandScale(level, 750, 1000, 1200, hpScaleAt750, hpScaleAt1000, hpScaleAt1200);

        obj.BaseHp = (long)Math.Round(obj.BaseHp * hpScale);
        obj.BonusHp = (long)Math.Round(obj.BonusHp * hpScale);
    }

    /// <summary>
    /// Assigns a random size category to the specified monster based on its level.
    /// </summary>
    /// <remarks>The assigned size is determined by the monster's level and a random roll. This method
    /// overwrites the current value of the monster's Size property.</remarks>
    /// <param name="obj">The monster instance whose size will be determined and set. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollMonsterSize(Monster obj)
    {
        var r = Generator.RandNumGen100();

        obj.Size = obj.Level switch
        {
            >= 1 and <= 11 => r switch
            {
                <= 35 => "Lessor",
                <= 60 => "Small",
                <= 85 => "Medium",
                _ => "Large"
            },
            <= 135 => r switch
            {
                <= 10 => "Lessor",
                <= 30 => "Small",
                <= 50 => "Medium",
                <= 70 => "Large",
                <= 90 => "Great",
                _ => "Colossal"
            },
            <= 250 => r switch
            {
                <= 7 => "Lessor",
                <= 20 => "Small",
                <= 35 => "Medium",
                <= 50 => "Large",
                <= 70 => "Great",
                <= 85 => "Colossal",
                _ => "Deity"
            },
            <= 500 => r switch
            {
                <= 5 => "Lessor",
                <= 15 => "Small",
                <= 30 => "Medium",
                <= 55 => "Large",
                <= 65 => "Great",
                <= 80 => "Colossal",
                _ => "Deity"
            },
            <= 750 => r switch
            {
                <= 3 => "Lessor",
                <= 10 => "Small",
                <= 20 => "Medium",
                <= 30 => "Large",
                <= 50 => "Great",
                <= 70 => "Colossal",
                _ => "Deity"
            },
            _ => r switch
            {
                <= 1 => "Lessor",
                <= 5 => "Small",
                <= 10 => "Medium",
                <= 30 => "Large",
                <= 50 => "Great",
                <= 70 => "Colossal",
                _ => "Deity"
            }
        };
    }

    /// <summary>
    /// Calculates and assigns bonus armor class and magic resistance values to the specified monster based on its
    /// level.
    /// </summary>
    /// <remarks>This method updates the BonusAc and BonusMr properties of the provided monster. The bonus
    /// values are determined by the monster's template level and predefined armor class ranges.</remarks>
    /// <param name="obj">The monster instance to which the bonus armor class and magic resistance values will be applied. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollArmorAndMrBonuses(Monster obj)
    {
        var lvl = obj.Template.Level;

        var (start, end) = FirstLE(MonsterArrays.ArmorClassRanges, lvl);
        obj.BonusAc = Generator.GenerateDeterminedNumberRange(start, end);

        var mrBonus = Generator.GenerateDeterminedNumberRange(start, end) * 2;
        obj.BonusMr = mrBonus;
    }

    /// <summary>
    /// Applies armor type-based adjustments to the specified monster's bonus armor class (AC) and magic resistance (MR)
    /// values.
    /// </summary>
    /// <remarks>The adjustments are determined by the monster's armor type as defined in its template. This
    /// method modifies the monster's BonusAc and BonusMr properties in place. The specific adjustment factors depend on
    /// whether the monster is classified as Common, Tank, or Caster.</remarks>
    /// <param name="obj">The monster whose bonus AC and MR values will be modified based on its armor type. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyArmorTypeAdjustments(Monster obj)
    {
        switch (obj.Template.MonsterArmorType)
        {
            default:
            case MonsterArmorType.Common:
                obj.BonusAc = (int)(obj.BonusAc * 0.75d);
                obj.BonusMr = (int)(obj.BonusMr * 0.75d);
                break;

            case MonsterArmorType.Tank:
                obj.BonusMr = (int)(obj.BonusMr * 0.30d);
                break;

            case MonsterArmorType.Caster:
                obj.BonusAc = (int)(obj.BonusAc * 0.35d);
                break;
        }
    }

    /// <summary>
    /// Assigns a randomized experience value to the specified monster based on its level.
    /// </summary>
    /// <remarks>The assigned experience is calculated using the monster's level and may vary within a range
    /// appropriate for that level. This method overwrites the monster's existing experience value.</remarks>
    /// <param name="obj">The monster whose experience value will be determined and set. Must not be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollExperience(Monster obj)
    {
        var lvl = obj.Template.Level;

        if (lvl <= ExperienceSeedLevel)
        {
            var (s, e) = FirstLE(MonsterArrays.ExperienceMilestones, lvl);
            var min = (long)(s * 0.9);
            var max = (long)(e * 1.1);
            obj.Experience = Generator.GenerateDeterminedLargeNumberRange(min, max);
            return;
        }

        var target = RoundUpToStep(lvl, ExperienceStep);
        if (target > ExperienceMaxLevel) target = ExperienceMaxLevel;

        // target in [205..1200] step 5; map to index
        if (target < ExperienceBaseLevel) target = ExperienceBaseLevel;
        var idx = (target - ExperienceBaseLevel) / ExperienceStep;

        var (start, end) = s_experience205Plus[idx];

        var minXp = (long)(start * 0.9);
        var maxXp = (long)(end * 1.1);
        obj.Experience = Generator.GenerateDeterminedLargeNumberRange(minXp, maxXp);
    }

    /// <summary>
    /// Calculates and assigns the ability value for the specified monster based on its level and template information.
    /// </summary>
    /// <remarks>This method updates the <see cref="Monster.Ability"/> property of the provided monster
    /// instance. The calculation is based on the monster's current level and template, and is only performed for
    /// monsters at level 250 or higher. For lower-level monsters, the ability value is set to zero.</remarks>
    /// <param name="obj">The monster whose ability value is to be determined and updated. Must not be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollAbility(Monster obj)
    {
        if (obj.Level < 250)
        {
            obj.Ability = 0;
            return;
        }

        var lvl = obj.Template.Level;

        if (lvl <= AbilitySeedLevel)
        {
            var (s, e) = FirstLE(MonsterArrays.AbilityMilestones, lvl);
            var min = (int)(s * 0.9);
            var max = (int)(e * 1.1);
            obj.Ability = (uint)Generator.GenerateDeterminedNumberRange(min, max);
            return;
        }

        var target = RoundUpToStep(lvl, AbilityStep);
        if (target > AbilityMaxLevel) target = AbilityMaxLevel;

        // target in [505..1200] step 5; map to index
        if (target < AbilityBaseLevel) target = AbilityBaseLevel;
        var idx = (target - AbilityBaseLevel) / AbilityStep;

        var (start, end) = s_ability505Plus[idx];

        var minXp = (int)(start * 0.9);
        var maxXp = (int)(end * 1.1);
        obj.Ability = (uint)Generator.GenerateDeterminedNumberRange(minXp, maxXp);
    }

    /// <summary>
    /// Assigns random elemental alignments to the offense and defense properties of the specified sprite based on its
    /// level.
    /// </summary>
    /// <remarks>The elemental alignment assigned to the sprite depends on its current level. Lower-level
    /// sprites receive alignments from a different set than higher-level sprites. This method overwrites any existing
    /// elemental alignment values on the sprite.</remarks>
    /// <param name="obj">The sprite whose offense and defense elemental alignments will be determined. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollElementalAlignment(Sprite obj)
    {
        var offRand = Generator.RandNumGen100();
        var defRand = Generator.RandNumGen100();

        switch (obj.Level)
        {
            case >= 1 and <= 11:
                obj.OffenseElement = PickElementLow(offRand);
                obj.DefenseElement = PickElementLow(defRand);
                return;

            case <= 98:
                obj.OffenseElement = PickElementMid(offRand);
                obj.DefenseElement = PickElementMid(defRand);
                return;

            case <= 250:
                obj.OffenseElement = PickElementHigh(offRand);
                obj.DefenseElement = PickElementHigh(defRand);
                return;

            default:
                obj.OffenseElement = PickElementEndgame(offRand);
                obj.DefenseElement = PickElementEndgame(defRand);
                return;
        }
    }

    /// <summary>
    /// Selects an element type based on the specified integer value.
    /// </summary>
    /// <remarks>The mapping of integer values to element types is based on predefined ranges. Values outside
    /// the expected range will return <see cref="ElementManager.Element.Holy"/>.</remarks>
    /// <param name="r">A non-negative integer used to determine which element type to select. Values correspond to specific element
    /// ranges.</param>
    /// <returns>An element from the <see cref="ElementManager.Element"/> enumeration corresponding to the range in which
    /// <paramref name="r"/> falls.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ElementManager.Element PickElementLow(int r) => r switch
    {
        <= 18 => ElementManager.Element.None,
        <= 36 => ElementManager.Element.Wind,
        <= 54 => ElementManager.Element.Earth,
        <= 72 => ElementManager.Element.Water,
        <= 90 => ElementManager.Element.Fire,
        <= 95 => ElementManager.Element.Void,
        _ => ElementManager.Element.Holy
    };

    /// <summary>
    /// Selects an element type based on the specified integer value, mapping ranges of values to corresponding
    /// elements.
    /// </summary>
    /// <remarks>Values of <paramref name="r"/> in the ranges 0–20, 21–40, 41–60, 61–80, and 81–90 map to
    /// Wind, Earth, Water, Fire, and Void elements, respectively. Values greater than 90 map to the Holy
    /// element.</remarks>
    /// <param name="r">An integer value used to determine which element type to select. Must be non-negative.</param>
    /// <returns>An element from the <see cref="ElementManager.Element"/> enumeration corresponding to the specified value of
    /// <paramref name="r"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ElementManager.Element PickElementMid(int r) => r switch
    {
        <= 20 => ElementManager.Element.Wind,
        <= 40 => ElementManager.Element.Earth,
        <= 60 => ElementManager.Element.Water,
        <= 80 => ElementManager.Element.Fire,
        <= 90 => ElementManager.Element.Void,
        _ => ElementManager.Element.Holy
    };

    /// <summary>
    /// Selects an element type based on the specified integer value.
    /// </summary>
    /// <remarks>The mapping from <paramref name="r"/> to element type is based on predefined value ranges.
    /// Values outside the documented ranges will return <see cref="ElementManager.Element.Holy"/>.</remarks>
    /// <param name="r">An integer value used to determine which element type to select.</param>
    /// <returns>An element from <see cref="ElementManager.Element"/> corresponding to the specified value of <paramref
    /// name="r"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ElementManager.Element PickElementHigh(int r) => r switch
    {
        <= 10 => ElementManager.Element.Wind,
        <= 20 => ElementManager.Element.Earth,
        <= 30 => ElementManager.Element.Water,
        <= 40 => ElementManager.Element.Fire,
        <= 75 => ElementManager.Element.Void,
        _ => ElementManager.Element.Holy
    };

    /// <summary>
    /// Selects an endgame element based on the specified integer value.
    /// </summary>
    /// <param name="r">An integer representing the selection value. Determines which element is returned based on predefined ranges.</param>
    /// <returns>An element from the <see cref="ElementManager.Element"/> enumeration corresponding to the specified value of
    /// <paramref name="r"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ElementManager.Element PickElementEndgame(int r) => r switch
    {
        <= 10 => ElementManager.Element.Wind,
        <= 20 => ElementManager.Element.Earth,
        <= 30 => ElementManager.Element.Water,
        <= 40 => ElementManager.Element.Fire,
        <= 60 => ElementManager.Element.Void,
        <= 80 => ElementManager.Element.Holy,
        <= 90 => ElementManager.Element.Rage,
        _ => ElementManager.Element.Sorrow
    };

    /// <summary>
    /// Rolls and assigns starting stat values for the specified monster based on its template level.
    /// </summary>
    /// <remarks>This method updates the monster's core stat fields (such as Strength, Intelligence, Wisdom,
    /// Constitution, and Dexterity) using a level-based range. Existing stat values will be overwritten.</remarks>
    /// <param name="obj">The monster instance whose starting stats will be generated and set. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RollStartingStats(Monster obj)
    {
        var lvl = obj.Template.Level;

        var (start, end) = FirstLE(MonsterArrays.StartingStatRanges, lvl);

        obj._Str = Generator.GenerateDeterminedNumberRange(start, end);
        obj._Int = Generator.GenerateDeterminedNumberRange(start, end);
        obj._Wis = Generator.GenerateDeterminedNumberRange(start, end);
        obj._Con = Generator.GenerateDeterminedNumberRange(start, end);
        obj._Dex = Generator.GenerateDeterminedNumberRange(start, end);
    }

    /// <summary>
    /// Applies stat modifications to a monster based on its size category, adjusting attributes such as Constitution,
    /// Strength, Dexterity, bonus hit points, experience, and ability points.
    /// </summary>
    /// <remarks>This method recalculates the monster's core stats and applies additional effects depending on
    /// the value of the Size property. If the size is not recognized, it defaults to the 'Lessor' category. The stat
    /// changes are randomized within defined ranges for each size category.</remarks>
    /// <param name="obj">The monster whose stats will be modified according to its size. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplySizeStatEffects(Monster obj)
    {
        var statGen1 = Random.Shared.Next(1, 3);
        var statGen2 = Random.Shared.Next(2, 4);
        var statGen3 = Random.Shared.Next(4, 7);
        var statGen4 = Random.Shared.Next(6, 11);

        RollStartingStats(obj);

        switch (obj.Size)
        {
            case "Lessor":
                obj._Con -= statGen1;
                obj._Str -= statGen1;
                obj._Dex -= statGen1;
                break;

            case "Small":
                obj._Con -= statGen1;
                obj._Str -= statGen1;
                obj._Dex += statGen2;
                break;

            case "Medium":
                obj._Con += statGen2;
                obj._Str += statGen2;
                obj._Dex += statGen2;
                break;

            case "Large":
                obj._Con += statGen3;
                obj._Str += statGen2;
                obj._Dex -= statGen1;
                obj.BonusHp += (long)(obj.MaximumHp * 0.012);
                obj.Experience += (uint)(obj.Experience * 0.02);
                obj.Ability += (uint)(obj.Ability * 0.02);
                break;

            case "Great":
                obj._Con += statGen3;
                obj._Str += statGen3;
                obj._Dex -= statGen2;
                obj.BonusHp += (long)(obj.MaximumHp * 0.024);
                obj.Experience += (uint)(obj.Experience * 0.06);
                obj.Ability += (uint)(obj.Ability * 0.06);
                break;

            case "Colossal":
                obj._Con += statGen3;
                obj._Str += statGen3;
                obj._Dex += statGen3;
                obj.BonusHp += (long)(obj.MaximumHp * 0.036);
                obj.Experience += (uint)(obj.Experience * 0.10);
                obj.Ability += (uint)(obj.Ability * 0.10);
                break;

            case "Deity":
                obj._Con += statGen4;
                obj._Str += statGen4;
                obj._Dex += statGen4;
                obj.BonusHp += (long)(obj.MaximumHp * 0.048);
                obj.Experience += (uint)(obj.Experience * 0.15);
                obj.Ability += (uint)(obj.Ability * 0.15);
                break;

            default:
                obj.Size = "Lessor";
                goto case "Lessor";
        }

        obj._Hit = (int)(obj._Dex * 0.2);
        obj.BonusHit = 10 * (obj.Template.Level / 12);
        obj.BonusMr = 10 * (obj.Template.Level / 14);
    }

    /// <summary>
    /// Applies a primary stat boost to the specified monster if its level is greater than 20.
    /// </summary>
    /// <remarks>This method randomly selects one of the monster's primary stats and increases related
    /// attributes accordingly. The boost is only applied if the monster's level is above 20. Calling this method
    /// multiple times may result in different primary stats being boosted each time.</remarks>
    /// <param name="obj">The monster to which the primary stat boost will be applied. Must not be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyPrimaryStatBoost(Monster obj)
    {
        if (obj.Level <= 20) return;

        var stat = Generator.RandomEnumValue<PrimaryStat>();
        obj.MajorAttribute = stat;

        switch (stat)
        {
            case PrimaryStat.STR:
                obj.BonusStr += (int)(obj._Str * 1.2);
                obj.BonusDmg += (int)(obj._Dmg * 1.2);
                break;

            case PrimaryStat.INT:
                obj.BonusInt += (int)(obj._Int * 1.2);
                obj.BonusMr += (int)(obj._Mr * 1.2);
                break;

            case PrimaryStat.WIS:
                obj.BonusWis += (int)(obj._Wis * 1.2);
                obj.BonusMp += (long)(obj.BaseMp * 1.2);
                break;

            case PrimaryStat.CON:
                obj.BonusCon += (int)(obj._Con * 1.2);
                obj.BonusHp += (long)(obj.BaseHp * 1.2);
                break;

            case PrimaryStat.DEX:
                obj.BonusDex += (int)(obj._Dex * 1.2);
                obj.BonusHit += (int)(obj._Hit * 1.2);
                break;
        }
    }

    /// <summary>
    /// Applies stat and attribute bonuses to the specified monster based on its type and level.
    /// </summary>
    /// <remarks>The bonuses applied depend on the monster's type as defined in its template. No changes are
    /// made if the monster's level is 20 or below.</remarks>
    /// <param name="obj">The monster to which type-based stat boosts will be applied. Must not be null. Only monsters with a level
    /// greater than 20 receive bonuses.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyTypeBoost(Monster obj)
    {
        if (obj.Level <= 20) return;

        switch (obj.Template.MonsterType)
        {
            case MonsterType.Physical:
                obj.BonusStr += (int)(obj._Str * 2);
                obj.BonusDex += (int)(obj._Dex * 1.2);
                obj.BonusDmg += (int)(obj._Dmg * 2);
                break;

            case MonsterType.Magical:
                obj.BonusInt += (int)(obj._Int * 2);
                obj.BonusWis += (int)(obj._Wis * 1.2);
                obj.BonusMr += (int)(obj._Hit * 2);
                break;

            case MonsterType.MiniBoss:
                obj.BonusStr += obj._Str * 15;
                obj.BonusInt += obj._Int * 15;
                obj.BonusWis += obj._Wis * 15;
                obj.BonusCon += obj._Con * 15;
                obj.BonusDex += obj._Dex * 15;
                obj.BonusMr += obj._Mr * 15;
                obj.BonusHit += obj._Hit * 10;
                obj.BonusDmg += obj._Dmg * 10;
                obj.BonusHp += obj.BaseHp * 20;
                obj.BonusMp += obj.BaseMp * 20;
                break;

            case MonsterType.Rift:
                obj.BonusStr += obj._Str * 5;
                obj.BonusInt += obj._Int * 5;
                obj.BonusDex += obj._Dex * 5;
                obj.BonusMr += obj._Mr * 5;
                obj.BonusHit += obj._Hit * 5;
                obj.BonusDmg += obj._Dmg * 5;
                break;

            case MonsterType.Boss:
                obj.BonusStr += obj._Str * 30;
                obj.BonusInt += obj._Int * 30;
                obj.BonusWis += obj._Wis * 30;
                obj.BonusCon += obj._Con * 30;
                obj.BonusDex += obj._Dex * 30;
                obj.BonusMr += obj._Mr * 10;
                obj.BonusHit += obj._Hit * 10;
                obj.BonusDmg += obj._Dmg * 10;
                obj.BonusHp += obj.BaseHp * 27;
                obj.BonusMp += obj.BaseMp * 27;
                break;
        }
    }

    /// <summary>
    /// Applies diminishing returns and caps to the bonus attributes of the specified monster based on its level.
    /// </summary>
    /// <remarks>This method only modifies bonus attributes if the monster's level is 20 or higher. After
    /// applying diminishing returns, certain bonuses are capped to predefined maximum values to prevent excessive
    /// scaling.</remarks>
    /// <param name="obj">The monster whose bonus attributes will be adjusted. Must not be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CapAndDiminishBonuses(Monster obj)
    {
        if (obj.Level < 20) return;

        obj.BonusStr = DiminishInt(obj._Str, obj.BonusStr, obj.Level);
        obj.BonusInt = DiminishInt(obj._Int, obj.BonusInt, obj.Level);
        obj.BonusDex = DiminishInt(obj._Dex, obj.BonusDex, obj.Level);
        obj.BonusWis = DiminishInt(obj._Wis, obj.BonusWis, obj.Level);
        obj.BonusCon = DiminishInt(obj._Con, obj.BonusCon, obj.Level);

        obj.BonusDmg = DiminishInt(obj._Dmg, obj.BonusDmg, obj.Level);
        obj.BonusHp = DiminishLong(obj.BaseHp, obj.BonusHp, obj.Level);
        obj.BonusMp = DiminishLong(obj.BaseMp, obj.BonusMp, obj.Level);

        obj.BonusHit = DiminishInt(obj._Hit, obj.BonusHit, obj.Level);
        obj.BonusMr = DiminishInt(obj._Mr, obj.BonusMr, obj.Level);
        obj.BonusAc = DiminishInt(obj._ac, obj.BonusAc, obj.Level);

        obj.BonusDmg = Math.Min(obj.BonusDmg, 500);
        obj.BonusWis = Math.Min(obj.BonusWis, 750);
        obj.BonusCon = Math.Min(obj.BonusCon, 750);

        obj.BonusHit = Math.Min(obj.BonusHit, 450);
        obj.BonusMr = Math.Min(obj.BonusMr, 450);
        obj.BonusAc = Math.Min(obj.BonusAc, 450);
    }

    /// <summary>
    /// Calculates a diminished integer value based on a base value, a bonus value, and a level-dependent diminishing
    /// curve.
    /// </summary>
    /// <remarks>This method applies a non-linear diminishing formula to the bonus value when it exceeds the
    /// base value, using a curve determined by the specified level. If the bonus value is less than or equal to the
    /// base value, or if the base value is zero or negative, the bonus value is returned without
    /// modification.</remarks>
    /// <param name="baseVal">The base value used as the reference point for diminishing. Must be greater than zero to apply diminishing;
    /// otherwise, the bonus value is returned.</param>
    /// <param name="bonusVal">The bonus value to be diminished. If less than or equal to the base value, this value is returned unchanged.</param>
    /// <param name="level">The level used to determine the diminishing curve. Higher levels may result in a different rate of diminishing.</param>
    /// <returns>An integer representing the diminished value, which will not be less than the base value or greater than <see
    /// cref="int.MaxValue"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DiminishInt(int baseVal, int bonusVal, int level)
    {
        if (bonusVal <= baseVal) return bonusVal;
        if (baseVal <= 0) return bonusVal;

        var ratio = (double)bonusVal / baseVal;
        var curve = GetStatDiminishCurve(level);
        var diminished = baseVal * Math.Pow(ratio, curve);

        if (diminished < baseVal) diminished = baseVal;
        if (diminished > int.MaxValue) diminished = int.MaxValue;

        return (int)Math.Round(diminished);
    }

    /// <summary>
    /// Calculates a diminished value for a bonus stat based on a base value and a diminishing curve determined by the
    /// specified level.
    /// </summary>
    /// <remarks>This method applies a non-linear diminishing formula to prevent bonus values from scaling
    /// excessively beyond the base value. The diminishing curve is determined by the specified level, allowing for
    /// adjustable scaling behavior. The result is clamped to the range between the base value and <see
    /// cref="long.MaxValue"/>.</remarks>
    /// <param name="baseVal">The base value used as the reference point for diminishing. Must be greater than zero to apply diminishing;
    /// otherwise, the bonus value is returned.</param>
    /// <param name="bonusVal">The bonus value to be diminished. If less than or equal to the base value, this value is returned unchanged.</param>
    /// <param name="level">The level used to determine the steepness of the diminishing curve. Higher levels typically result in stronger
    /// diminishing effects.</param>
    /// <returns>A long integer representing the diminished bonus value. Returns the original bonus value if it is less than or
    /// equal to the base value, or if the base value is not positive.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DiminishLong(long baseVal, long bonusVal, int level)
    {
        if (bonusVal <= baseVal) return bonusVal;
        if (baseVal <= 0) return bonusVal;

        var ratio = (double)bonusVal / baseVal;
        var curve = GetStatDiminishCurve(level);
        var diminished = baseVal * Math.Pow(ratio, curve);

        if (diminished < baseVal) diminished = baseVal;
        if (diminished > long.MaxValue) diminished = long.MaxValue;

        return (long)Math.Round(diminished);
    }

    /// <summary>
    /// Calculates the diminishing curve multiplier for a stat based on the specified level.
    /// </summary>
    /// <remarks>This method is typically used to apply diminishing returns to stat scaling as the level
    /// increases. The returned multiplier decreases linearly between level 750 and 1200.</remarks>
    /// <param name="level">The stat level for which to calculate the diminishing curve. Must be greater than or equal to 0.</param>
    /// <returns>A double value representing the diminishing multiplier for the given level. Returns 0.90 for levels less than or
    /// equal to 750, 0.80 for levels greater than or equal to 1200, and a linearly interpolated value between 0.90 and
    /// 0.80 for levels in between.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetStatDiminishCurve(int level)
    {
        if (level <= 750) return 0.90;
        if (level >= 1200) return 0.80;

        var t = (level - 750) / 450.0;
        return Lerp(0.90, 0.80, t);
    }

    /// <summary>
    /// Calculates a logarithmically interpolated scale value for a specified level, based on three reference levels and
    /// their corresponding scale values.
    /// </summary>
    /// <remarks>This method performs logarithmic interpolation between the provided scale values. It is
    /// typically used when scale transitions should appear smooth on a logarithmic scale, such as in audio or signal
    /// processing applications.</remarks>
    /// <param name="level">The level for which to calculate the scale value.</param>
    /// <param name="aLevel">The lower reference level. Must be less than or equal to <paramref name="bLevel"/>.</param>
    /// <param name="bLevel">The middle reference level. Must be between <paramref name="aLevel"/> and <paramref name="cLevel"/>.</param>
    /// <param name="cLevel">The upper reference level. Must be greater than or equal to <paramref name="bLevel"/>.</param>
    /// <param name="aScale">The scale value associated with <paramref name="aLevel"/>.</param>
    /// <param name="bScale">The scale value associated with <paramref name="bLevel"/>.</param>
    /// <param name="cScale">The scale value associated with <paramref name="cLevel"/>.</param>
    /// <returns>A scale value interpolated between <paramref name="aScale"/>, <paramref name="bScale"/>, and <paramref
    /// name="cScale"/> according to the position of <paramref name="level"/> relative to the reference levels. Returns
    /// <paramref name="aScale"/> if <paramref name="level"/> is less than or equal to <paramref name="aLevel"/>, and
    /// <paramref name="cScale"/> if <paramref name="level"/> is greater than or equal to <paramref name="cLevel"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetBandScale(int level, int aLevel, int bLevel, int cLevel, double aScale, double bScale, double cScale)
    {
        if (level <= aLevel) return aScale;
        if (level >= cLevel) return cScale;

        if (level <= bLevel)
        {
            var t = (level - aLevel) / (double)(bLevel - aLevel);
            return LogLerp(aScale, bScale, t);
        }

        var t2 = (level - bLevel) / (double)(cLevel - bLevel);
        return LogLerp(bScale, cScale, t2);
    }

    /// <summary>
    /// Performs a logarithmic interpolation between two positive values.
    /// </summary>
    /// <remarks>Logarithmic interpolation is useful for smoothly blending values that span several orders of
    /// magnitude, such as audio volumes or physical quantities. If 'a' or 'b' is less than or equal to zero, a small
    /// positive value is substituted to ensure a valid result.</remarks>
    /// <param name="a">The start value. Must be greater than zero to avoid undefined logarithmic behavior.</param>
    /// <param name="b">The end value. Must be greater than zero to avoid undefined logarithmic behavior.</param>
    /// <param name="t">The interpolation factor, typically in the range [0.0, 1.0]. Values outside this range are clamped.</param>
    /// <returns>A value between 'a' and 'b', interpolated logarithmically according to 't'.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LogLerp(double a, double b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        if (a <= 0) a = 0.000001;
        if (b <= 0) b = 0.000001;

        var la = Math.Log(a);
        var lb = Math.Log(b);
        return Math.Exp(la + (lb - la) * t);
    }

    /// <summary>
    /// Linearly interpolates between two values based on a weighting factor.
    /// </summary>
    /// <param name="a">The value to interpolate from when the weighting factor is 0.0.</param>
    /// <param name="b">The value to interpolate to when the weighting factor is 1.0.</param>
    /// <param name="t">The interpolation factor, typically in the range [0.0, 1.0]. Values less than 0.0 are clamped to 0.0; values
    /// greater than 1.0 are clamped to 1.0.</param>
    /// <returns>A value that is the linear interpolation between 'a' and 'b' at the specified weighting factor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Lerp(double a, double b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return a + (b - a) * t;
    }

    /// <summary>
    /// Rounds the specified value up to the nearest multiple of the given step.
    /// </summary>
    /// <param name="value">The integer value to be rounded up.</param>
    /// <param name="step">The step size to which the value is rounded. Must be greater than zero.</param>
    /// <returns>The smallest integer greater than or equal to value that is a multiple of step. If step is less than or equal to
    /// 1, returns value unchanged.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpToStep(int value, int step)
    {
        if (step <= 1) return value;
        var rem = value % step;
        return rem == 0 ? value : value + (step - rem);
    }
}
