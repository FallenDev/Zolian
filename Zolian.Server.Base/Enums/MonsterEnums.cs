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
    GodlyStr,
    GodlyInt,
    GodlyWis,
    GodlyCon,
    GodlyDex,
    Above99P,
    Above99M,
    Above150P,
    Above150M,
    Above200P,
    Above200M,
    Above250P,
    Above250M,
    MasterStr,
    MasterInt,
    MasterWis,
    MasterCon,
    MasterDex,
    Forsaken,
    Above300P,
    Above300M,
    Above350P,
    Above350M,
    Above400P,
    Above400M,
    Above450P,
    Above450M,
    DivineStr,
    DivineInt,
    DivineWis,
    DivineCon,
    DivineDex,
    MiniBoss,
    Boss
}

/// <summary>
/// Common - 75% AC & Will Save
/// Tank - 100% AC & 30% Will Save
/// Caster - 30% AC & 100% Will Save
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