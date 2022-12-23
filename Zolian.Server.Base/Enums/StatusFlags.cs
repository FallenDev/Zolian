namespace Darkages.Enums
{
    [Flags]
    public enum StatusFlags : byte
    {
        UnreadMail = 0x01,
        Unknown = 0x02,
        StructA = 0x20,
        StructB = 0x10,
        StructC = 0x08,
        StructD = 0x04,
        GameMasterA = 0x40,
        GameMasterB = 0x80,
        Swimming = GameMasterA | GameMasterB,
        MultiStat = StructA | StructB | StructD,
        WeightMoney = StructA | StructC,
        Health = StructA | StructB,
        ExpSpend = StructA | StructB | StructC,
        All = StructA | StructB | StructC | StructD
    }

    [Flags]
    public enum StatusBarColor : byte
    {
        Off = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Orange = 4,
        Red = 5,
        White = 6
    }
}