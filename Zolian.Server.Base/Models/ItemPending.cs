namespace Darkages.Models;

public class PendingSell
{
    public uint ID { get; init; }
    public string Name { get; init; }
    public ushort Quantity { get; set; }
}

public class PendingBuy
{
    public uint ID { get; init; }
    public string Name { get; init; }
    public int Offer { get; init; }
    public ushort Quantity { get; set; }
}