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

    public static Nation NationValue(string e)
    {
        return e switch
        {
            "Exile" => Nation.Exile,
            "Suomi" => Nation.Suomi,
            "Ellas" => Nation.Ellas,
            "Loures" => Nation.Loures,
            "Mileth" => Nation.Mileth,
            "Tagor" => Nation.Tagor,
            "Rucesion" => Nation.Rucesion,
            "Noes" => Nation.Noes,
            "Illuminati" => Nation.Illuminati,
            "Piet" => Nation.Piet,
            "Atlantis" => Nation.Atlantis,
            "Abel" => Nation.Abel,
            "Undine" => Nation.Undine,
            "Purgatory" => Nation.Purgatory,
            _ => Nation.Exile
        };
    }
}
