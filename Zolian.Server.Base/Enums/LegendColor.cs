namespace Darkages.Enums;

public enum LegendColor
{
    Aqua = 1,
    White = 32,
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
    LightPink = 96,
    DarkPurple = 100,
    Lavender = 105,
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
            LegendColor.Aqua => 1,
            LegendColor.White => 32,
            LegendColor.Pink => 36,
            LegendColor.Peony => 38,
            LegendColor.LightOrange => 50,
            LegendColor.Mahogany => 63,
            LegendColor.Brass => 64,
            LegendColor.LightYellow => 68,
            LegendColor.LightGreen => 75,
            LegendColor.Teal => 87,
            LegendColor.Blue => 88,
            LegendColor.LightPink => 96,
            LegendColor.DarkPurple => 100,
            LegendColor.Lavender => 105,
            LegendColor.Green => 128,
            LegendColor.Orange => 152,
            LegendColor.Brown => 160,
            LegendColor.Red => 248,
            _ => 88
        };
    }
}