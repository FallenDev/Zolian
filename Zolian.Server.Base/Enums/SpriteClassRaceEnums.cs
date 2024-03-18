﻿namespace Darkages.Enums;

[Flags]
public enum Class
{
    Peasant = 1,
    Berserker = 1 << 1,
    Defender = 1 << 2,
    Assassin = 1 << 3,
    Cleric = 1 << 4,
    Arcanus = 1 << 5,
    Monk = 1 << 6,
    DualBash = Berserker | Defender,
    DualCast = Cleric | Arcanus,
    Racial = 1 << 7,
    Monster = 1 << 8,
    Quest = 1 << 9
}

[Flags]
public enum Job
{
    None = 0,
    Thief = 1, // ♈︎
    DarkKnight = 1 << 1, // ♒︎
    Templar = 1 << 2, // ♉︎
    Ninja = 1 << 3, // ♍︎
    SharpShooter = 1 << 4, // ♌︎
    Oracle = 1 << 5, // ♎︎
    Bard = 1 << 6, // ♏︎
    Summoner = 1 << 7, // ♊︎
    Samurai = 1 << 8, // ♓︎
    ShaolinMonk = 1 << 9, // ♋︎
    Necromancer = 1 << 10, // ♑︎
    Dragoon = 1 << 11 // ♐︎
}

[Flags]
public enum Race
{
    UnDecided = 0,
    Human = 1,
    HalfElf = 2,
    HighElf = 3,
    DarkElf = 4,
    WoodElf = 5,
    Orc = 6,
    Dwarf = 7,
    Halfling = 8,
    Dragonkin = 9,
    HalfBeast = 10,
    Merfolk = 11
}

[Flags]
public enum ClassStage
{
    Class = 1,
    Dedicated = 1 << 1,
    Advance = 1 << 2,
    Master = 1 << 3,
    Job = 1 << 4 | Master,
    Quest = 1 << 5,

    MasterLearn = Class | Dedicated | Advance | Master,
    DedicatedLearn = Class | Dedicated,
    AdvanceLearn = Class | Advance,
    ForsakenLearn = Class | Dedicated | Advance | Master | Job
}

[Flags]
public enum Afflictions
{
    Normal = 1,
    Lycanisim = 1 << 1,
    Vampirisim = 1 << 2,
    Plagued = 1 << 3, // -500 hp -500 mp -5 all stats
    TheShakes = 1 << 4, // -5 Con, -5 Dex, -50 dmg
    Stricken = 1 << 5, // -1500 mp, -10 Wis, -10 Regen
    Rabies = 1 << 6, // Death if not cured in an hour
    LockJoint = 1 << 7, // -30 dmg, random beag suain
    NumbFall = 1 << 8, // -20 dmg, -20 hit
    Diseased = Plagued | TheShakes,
    Hallowed = Plagued | Stricken,
    Petrified = LockJoint | NumbFall
}

[Flags]
public enum SubClassDragonkin
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Black = 3,
    White = 4,
    Brass = 5,
    Bronze = 6,
    Copper = 7,
    Gold = 8,
    Silver = 9
}

public static class SpriteClassRaceExtensions
{
    public static bool ClassFlagIsSet(this Class self, Class flag) => (self & flag) == flag;
    public static bool JobFlagIsSet(this Job self, Job flag) => (self & flag) == flag;
    public static bool RaceFlagIsSet(this Race self, Race flag) => (self & flag) == flag;
    public static bool StageFlagIsSet(this ClassStage self, ClassStage flag) => (self & flag) == flag;
    public static bool AfflictionFlagIsSet(this Afflictions self, Afflictions flag) => (self & flag) == flag;
    public static bool SubClassFlagIsSet(this SubClassDragonkin self, SubClassDragonkin flag) => (self & flag) == flag;
}

public static class ClassStrings
{
    public static string ClassValue(Class c)
    {
        return c switch
        {
            Class.Peasant => "Peasant",
            Class.Berserker => "Berserker",
            Class.Defender => "Defender",
            Class.Assassin => "Assassin",
            Class.Cleric => "Cleric",
            Class.Arcanus => "Arcanus",
            Class.Monk => "Monk",
            Class.DualBash => "DualBash",
            Class.DualCast => "DualCast",
            Class.Racial => "Racial",
            Class.Monster => "Monster",
            Class.Quest => "Quest",
            _ => "Peasant"
        };
    }

    public static int ClassDisplayInt(string c)
    {
        return c switch
        {
            "Peasant" => 0,
            "Berserker" => 1,
            "Defender" => 2,
            "Assassin" => 3,
            "Cleric" => 4,
            "Arcanus" => 5,
            "Monk" => 6,
            "DualBash" => 7,
            "DualCast" => 8,
            "Racial" => 9,
            "Monster" => 10,
            "Quest" => 11,
            _ => 0
        };
    }

    public static int ItemClassToIntMetaData(string c)
    {
        return c switch
        {
            "Peasant" => 0,
            "Berserker" => 1,
            "Defender" => 1,
            "Assassin" => 2,
            "Cleric" => 4,
            "Arcanus" => 3,
            "Monk" => 5,
            "DualBash" => 0,
            "DualCast" => 0,
            "Racial" => 0,
            "Monster" => 0,
            "Quest" => 0,
            _ => 0
        };
    }

    public static int JobDisplayFlag(string c)
    {
        return c switch
        {
            "None" => 0,
            "Thief" => 1,
            "Dark Knight" => 2,
            "Templar" => 4,
            "Knight" => 8,
            "Ninja" => 16,
            "SharpShooter" => 32,
            "Oracle" => 64,
            "Bard" => 128,
            "Summoner" => 256,
            "Samurai" => 512,
            "ShaolinMonk" => 1024,
            "Necromancer" => 2048,
            "Dragoon" => 4096,
            _ => 0
        };
    }

    public static string StageValue(ClassStage c)
    {
        return c switch
        {
            ClassStage.Class => "Class",
            ClassStage.Dedicated => "Dedicated",
            ClassStage.Advance => "Advance",
            ClassStage.Master => "Master",
            ClassStage.Job => "Job",
            ClassStage.Quest => "Quest",
            ClassStage.MasterLearn => "MasterLearn",
            ClassStage.DedicatedLearn => "DedicatedLearn",
            ClassStage.AdvanceLearn => "AdvanceLearn",
            ClassStage.ForsakenLearn => "ForsakenLearn",
            _ => "Class"
        };
    }

    public static string RaceValue(Race r)
    {
        return r switch
        {
            Race.UnDecided => "UnDecided",
            Race.Human => "Human",
            Race.HalfElf => "Half-Elf",
            Race.HighElf => "High Elf",
            Race.DarkElf => "Drow",
            Race.WoodElf => "Wood Elf",
            Race.Orc => "Orc",
            Race.Dwarf => "Dwarf",
            Race.Halfling => "Halfling",
            Race.Dragonkin => "Dragonkin",
            Race.HalfBeast => "Half-Beast",
            Race.Merfolk => "Merfolk",
            _ => "UnDecided"
        };
    }

    public static string AfflictionValue(Afflictions a)
    {
        return a switch
        {
            Afflictions.Normal => "Normal",
            Afflictions.Lycanisim => "Lycanisim",
            Afflictions.Vampirisim => "Vampirisim",
            Afflictions.Plagued => "Plagued",
            Afflictions.TheShakes => "The Shakes",
            Afflictions.Stricken => "Stricken",
            Afflictions.Rabies => "Rabies",
            Afflictions.LockJoint => "Lock Joint",
            Afflictions.NumbFall => "Numb Fall",
            Afflictions.Diseased => "Diseased",
            Afflictions.Hallowed => "Hallowed",
            Afflictions.Petrified => "Petrified",
            _ => "Normal"
        };
    }

    public static string SubRaceDragonkinValue(SubClassDragonkin s)
    {
        return s switch
        {
            SubClassDragonkin.Red => "Red",
            SubClassDragonkin.Blue => "Blue",
            SubClassDragonkin.Green => "Green",
            SubClassDragonkin.Black => "Black",
            SubClassDragonkin.White => "White",
            SubClassDragonkin.Brass => "Brass",
            SubClassDragonkin.Bronze => "Bronze",
            SubClassDragonkin.Copper => "Copper",
            SubClassDragonkin.Gold => "Gold",
            SubClassDragonkin.Silver => "Silver",
            _ => "Red"
        };
    }
}