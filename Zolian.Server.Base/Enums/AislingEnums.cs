namespace Darkages.Enums;

public enum BodySprite : byte
{
    Male = 0x10,
    Female = 0x20,
    MaleGhost = 0x30,
    FemaleGhost = 0x40,
    MaleInvis = 0x50,
    FemaleInvis = 0x60,
    MaleJester = 0x70,
    MaleHead = 0x80,
    FemaleHead = 0x90,
    BlankMale = 0xA0,
    BlankFemale = 0xB0
}

[Flags]
public enum Gender : byte
{
    Male = 1,
    Female = 2,
    Unisex = Male | Female
}

public enum BodyColor : byte
{
    White = 0,
    Pale = 1,
    Brown = 2,
    Green = 3,
    Yellow = 4,
    Tan = 5,
    Grey = 6,
    LightBlue = 7,
    Orange = 8,
    Purple = 9
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

public enum LanternSize : byte
{
    None = 0,
    Small = 1,
    Large = 2
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

public static class PlayerExtensions
{
    public static bool PlayerFlagIsSet(this AislingFlags self, AislingFlags flag) => (self & flag) == flag;
}