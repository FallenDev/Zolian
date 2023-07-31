using Darkages.Sprites;

namespace Darkages.Models;

public class PendingSell
{
    public uint ID { get; init; }
    public string Name { get; init; }
    public uint Offer { get; set; }
    public ushort Quantity { get; set; }
    public ushort Removing { get; set; }
}

public class PendingBuy
{
    public string Name { get; init; }
    public int Offer { get; init; }
    public ushort Quantity { get; set; }
}