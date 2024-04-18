namespace Darkages.Enums;

public static class SpriteMaker
{
    public static string GenderValue(Gender e)
    {
        return e switch
        {
            Gender.Male => "Male",
            Gender.Female => "Female",
            _ => "Male"
        };
    }

    public static string NationValue(Nation e)
    {
        return e switch
        {
            Nation.Exile => "Exile",
            Nation.Suomi => "Suomi",
            Nation.Ellas => "Ellas",
            Nation.Loures => "Loures",
            Nation.Mileth => "Mileth",
            Nation.Tagor => "Tagor",
            Nation.Rucesion => "Rucesion",
            Nation.Noes => "Noes",
            Nation.Illuminati => "Illuminati",
            Nation.Piet => "Piet",
            Nation.Atlantis => "Atlantis",
            Nation.Abel => "Abel",
            Nation.Undine => "Undine",
            Nation.Purgatory => "Purgatory",
            _ => "Exile"
        };
    }

    public static string NationMetafileProfileDisplay(Nation e)
    {
        return e switch
        {
            Nation.Exile => "Exile",
            Nation.Suomi => "Suomi",
            Nation.Ellas => "Evermore",
            Nation.Loures => "Game Master",
            Nation.Mileth => "Mileth",
            Nation.Tagor => "Tagor",
            Nation.Rucesion => "Rucesion",
            Nation.Noes => "Loures",
            Nation.Illuminati => "Rionnag",
            Nation.Piet => "Piet",
            Nation.Atlantis => "Atlantis",
            Nation.Abel => "Abel",
            Nation.Undine => "Undine",
            Nation.Purgatory => "Outer Plane",
            _ => "Exile"
        };
    }
}
