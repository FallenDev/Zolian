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

public class PendingBanked
{
    // Item parsed on click
    public Item SelectedItem;
    public uint ItemId { get; set; }
    // Item's inventory slot on click
    public byte InventorySlot { get; set; }
    // Selected quantity for stacked items
    public ushort ArgsQuantity { get; set; }
    // ??
    public ushort BankQuantity { get; set; }
    public uint Cost { get; set; }
    public uint TempGold { get; set; }
    public bool DepositGold { get; set; }
    public bool DepositStackedItem { get; set; }
    public bool WithdrawGold { get; set; }
    public bool WithdrawItem { get; set; }
}