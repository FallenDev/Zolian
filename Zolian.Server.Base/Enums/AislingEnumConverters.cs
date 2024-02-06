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
}
