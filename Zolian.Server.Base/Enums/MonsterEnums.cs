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
    Forsaken,
    Boss
}

public enum MonsterRace
{
    None,
    Aquatic,
    Beast,
    DemiGod,
    Demon,
    Dragon,
    Drow,
    Dwarf,
    Dummy,
    Elemental,
    Fairy,
    Fungi,
    Gargoyle,
    Goblin,
    Grimlok,
    Humanoid,
    Inanimate,
    Insect,
    Kobold,
    Mukul,
    Ooze,
    Orc,
    Plant,
    Reptile,
    Robotic,
    Rodent,
    Undead
}

[Flags]
public enum MoodQualifer
{
    Idle = 1,
    Aggressive = 2,
    Unpredicable = 4,
    Neutral = 8,
    VeryAggressive = 16
}

public static class MonsterExtensions
{
    public static bool MoodFlagIsSet(this MoodQualifer self, MoodQualifer flag) => (self & flag) == flag;
}