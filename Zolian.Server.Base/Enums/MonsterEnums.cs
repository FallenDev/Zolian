namespace Darkages.Enums;

public enum MonsterEnums
{
    Pure,
    Elemental,
    Physical
}

public enum MonsterType
{
    None,
    Physical,
    Magical,
    MiniBoss,
    Rift,
    Boss
}

/// <summary>
/// Common - 55% AC & 75% Will Save
/// Tank - 70% AC & 50% Will Save
/// Caster - 40% AC & 90% Will Save
/// <see cref="GameScripts.Formulas.WillSavingThrow" />
/// <see cref="GameScripts.Formulas.Ac" />
/// </summary>
public enum MonsterArmorType
{
    Common,
    Tank,
    Caster
}

[Flags]
public enum MonsterRace
{
    None = 0,
    Aberration = 1,
    Animal = 1 << 1,
    Aquatic = 1 << 2,
    Beast = 1 << 3,
    Celestial = 1 << 4,
    Construct = 1 << 5,
    Demon = 1 << 6,
    Dragon = 1 << 7,
    Bahamut = Dragon + 1,
    Dummy = 1 << 8,
    Elemental = 1 << 9,
    Fairy = 1 << 10,
    Fiend = 1 << 11,
    Fungi = 1 << 12,
    Gargoyle = 1 << 13,
    Giant = 1 << 14,
    Goblin = 1 << 15,
    Grimlok = 1 << 16,
    Humanoid = 1 << 17,
    ShapeShifter = 1 << 18,
    Insect = 1 << 19,
    Kobold = 1 << 20,
    Magical = 1 << 21,
    Mukul = 1 << 22,
    Ooze = 1 << 23,
    Orc = 1 << 24,
    Plant = 18 << 25,
    Reptile = 1 << 26,
    Robotic = 1 << 27,
    Shadow = 1 << 28,
    Rodent = 1 << 29,
    Undead = 1 << 30,

    LowerBeing = Animal | Insect | Plant | Rodent,
    HigherBeing = Aberration | Celestial | Demon | Dragon | Fairy | Shadow
}

[Flags]
public enum MoodQualifer
{
    Idle = 1,
    Aggressive = 1 << 1,
    Unpredicable = 1 << 2,
    Neutral = 1 << 3,
    VeryAggressive = 1 << 4,
    Avoidance = 1 << 5
}

public static class MonsterExtensions
{
    public static bool MoodFlagIsSet(this MoodQualifer self, MoodQualifer flag) => (self & flag) == flag;
    public static bool MonsterRaceIsSet(this MonsterRace self, MonsterRace flag) => (self & flag) == flag;
    public static bool MonsterTypeIsSet(this MonsterType self, MonsterType flag) => (self & flag) == flag;
}