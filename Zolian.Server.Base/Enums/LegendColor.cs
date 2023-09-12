namespace Darkages.Enums;

public enum LegendColor
{
    Invisible = 0,
    Cyan = 1,
    BrightRed = 2,
    GrayTan = 3,
    BrightGray = 4,
    Gray = 5,
    OffWhite = 13,
    DarkGray = 14,
    White = 16,
    BrightBrightGray = 17,
    GrayGreen = 20,
    LightPink = 32,
    Pink = 36,
    Peony = 38,
    LightOrange = 50,
    Mahogany = 53,
    Brass = 63,
    LightYellow = 64,
    Yellow = 68,
    LightGreen = 75,
    Teal = 87,
    Blue = 88,
    LightPurple = 96,
    DarkPurple = 100,
    Lavender = 105,
    DarkGreen = 125,
    Green = 128,
    Orange = 152,
    Brown = 160,
    Red = 248
}

public static class LegendColorConverter
{
    public static int ColorToInt(LegendColor e)
    {
        return e switch
        {
            LegendColor.Invisible => 0,
            LegendColor.Cyan => 1,
            LegendColor.BrightRed => 2,
            LegendColor.GrayTan => 3,
            LegendColor.BrightGray => 4,
            LegendColor.Gray => 5,
            LegendColor.OffWhite => 13,
            LegendColor.DarkGray => 14,
            LegendColor.White => 16,
            LegendColor.BrightBrightGray => 17,
            LegendColor.GrayGreen => 20,
            LegendColor.LightPink => 32,
            LegendColor.Pink => 36,
            LegendColor.Peony => 38,
            LegendColor.LightOrange => 50,
            LegendColor.Mahogany => 53,
            LegendColor.Brass => 63,
            LegendColor.LightYellow => 64,
            LegendColor.Yellow => 68,
            LegendColor.LightGreen => 75,
            LegendColor.Teal => 87,
            LegendColor.Blue => 88,
            LegendColor.LightPurple => 96,
            LegendColor.DarkPurple => 100,
            LegendColor.Lavender => 105,
            LegendColor.DarkGreen => 125,
            LegendColor.Green => 128,
            LegendColor.Orange => 152,
            LegendColor.Brown => 160,
            LegendColor.Red => 248,
            _ => 0
        };
    }
}