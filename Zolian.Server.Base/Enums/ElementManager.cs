namespace Darkages.Enums;

public class ElementManager
{
    public enum Element
    {
        None = 0x00,
        Fire = 0x01,
        Water = 0x02,
        Wind = 0x03,
        Earth = 0x04,
        Holy = 0x05,
        Void = 0x06,
        Rage = 0x07,
        Terror = 0x08,
        Sorrow = 0x09
    }

    public static string ElementValue(Element e)
    {
        return e switch
        {
            Element.None => "None",
            Element.Fire => "Fire",
            Element.Water => "Water",
            Element.Wind => "Wind",
            Element.Earth => "Earth",
            Element.Holy => "Holy",
            Element.Void => "Void",
            Element.Rage => "Rage",
            Element.Terror => "Terror",
            Element.Sorrow => "Sorrow",
            _ => "None"
        };
    }
}