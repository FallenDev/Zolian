namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat50 : NetworkFormat
{
    /// <summary>
    /// Manufacture
    /// </summary>
    public ServerFormat50()
    {
        Encrypted = true;
        Command = 0x50;
    }

    public bool IsInitial { get; set; }
    public byte RecipeCount { get; set; } = 1;
    public byte Index { get; set; }
    public ushort Sprite { get; set; } = 50;
    public string RecipeName { get; set; } = new string("Spaghetti");
    public string RecipeDescription { get; set; } = new ("");
    public Dictionary<string, int> RecipeIngredients { get; set; } = new Dictionary<string, int>();

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        RecipeIngredients.Add("Spaghetti", 5);
        RecipeIngredients.Add("Meatballs", 20);
        RecipeIngredients.Add("Grandma's Sauce", 1);
        writer.Write((byte)0x01);
        writer.Write((byte)0x3C);

        if (IsInitial)
        {
            writer.Write((byte)0x00);
            writer.Write(RecipeCount);
            writer.Write((byte)0x00);
        }
        else
        {
            writer.Write((byte)0x01);
            writer.Write(Index);
            writer.Write(Sprite);
            writer.WriteStringB(RecipeDescription);

            var ingredients = " Ingredients: \n";

            foreach (var (key, value) in RecipeIngredients)
            {
                ingredients += $" {value} {key}\n";
            }
            writer.WriteStringB(ingredients);
            writer.Write((byte)0x01);
            writer.Write((byte)0x00);
        }
    }
}