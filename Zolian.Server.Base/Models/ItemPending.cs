namespace Darkages.Models;

public class PendingSell
{
    public long ID { get; set; }
    public string Name { get; init; }
    public ushort Quantity { get; set; }
}

public class PendingBuy
{
    public long ID { get; init; }
    public string Name { get; init; }
    public int Offer { get; init; }
    public ushort Quantity { get; set; }
}