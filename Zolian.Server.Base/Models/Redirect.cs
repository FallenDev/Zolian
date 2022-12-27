namespace Darkages.Models;

public class Redirect
{
    public string Name { get; init; }
    public string Salt { get; init; }
    public string Seed { get; init; }
    public string Serial { get; init; }
    public string Type { get; set; }
}