using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.Models;

public class ExchangeSession(Aisling user)
{
    public bool Confirmed { get; set; }
    public uint Gold { get; set; }
    public List<Item> Items { get; } = new();
    public Aisling Trader { get; } = user;
    public int Weight { get; set; }
}