﻿namespace Darkages.Enums;

[Flags]
public enum Gender : byte
{
    Male = 1,
    Female = 2,
    Unisex = Male | Female
}

public enum ListColor : byte
{
    Brown = 0xA7,
    DarkGray = 0xB7,
    Gray = 0x17,
    Green = 0x80,
    None = 0x00,
    Orange = 0x97,
    Red = 0x04,
    Tan = 0x30,
    Teal = 0x01,
    White = 0x90,
    Clan = 0x54,
    Me = 0x70
}

public enum RestPosition : byte
{
    Standing = 0x00,
    RestPosition1 = 0x01,
    RestPosition2 = 0x02,
    MaximumChill = 0x03
}

public enum GroupStatus
{
    NotAcceptingRequests = 0,
    AcceptingRequests = 1
}

public enum LegendIcon
{
    Community = 0,
    Warrior = 1,
    Rogue = 2,
    Wizard = 3,
    Priest = 4,
    Monk = 5,
    Heart = 6,
    Victory = 7
}

[Flags]
public enum AislingFlags
{
    Normal = 0,
    Ghost = 1
}

public enum AnimalForm : byte
{
    None = 0,
    Draco = 1,
    Kelberoth = 2,
    WhiteBat = 3,
    Scorpion = 4
}

public enum Mail : byte
{
    None = 0,
    Parcel = 1,
    Letter = 16
}

public enum NameDisplayStyle : byte
{
    GreyHover = 0x00,
    RedAlwaysOn = 0x01,
    GreenHover = 0x02,
    GreyAlwaysOn = 0x03
}

public enum ActivityStatus : byte
{
    Awake = 0,
    DoNotDisturb = 1,
    DayDreaming = 2,
    NeedGroup = 3,
    Grouped = 4,
    LoneHunter = 5,
    GroupHunter = 6,
    NeedHelp = 7
}

[Flags]
public enum Nation : byte
{
    Exile,
    Suomi,
    Ellas,
    Loures,
    Mileth,
    Tagor,
    Rucesion,
    Noes,
    Illuminati,
    Piet,
    Atlantis,
    Abel,
    Undine,
    Purgatory
}

public static class PlayerExtensions
{
    public static bool PlayerFlagIsSet(this AislingFlags self, AislingFlags flag) => (self & flag) == flag;
    public static bool PlayerNationFlagIsSet(this Nation self, Nation flag) => (self & flag) == flag;
}