using Darkages.Sprites;

namespace Darkages.Models;

public class PendingSell
{
    public int ID { get; init; }
    public string Name { get; init; }
    public uint Offer { get; set; }
    public int Quantity { get; set; }
    public int Removing { get; set; }
}

public class PendingBuy
{
    public string Name { get; init; }
    public int Offer { get; init; }
    public int Quantity { get; set; }
}

public class PendingBanked
{
    // Item parsed on click
    public Item SelectedItem;
    public int ItemId { get; set; }
    // Item's inventory slot on click
    public int InventorySlot { get; set; }
    // Selected quantity for stacked items
    public int ArgsQuantity { get; set; }
    // ??
    public int BankQuantity { get; set; }
    public uint Cost { get; set; }
    public uint TempGold { get; set; }
    public bool DepositGold { get; set; }
    public bool DepositStackedItem { get; set; }
    public bool WithdrawGold { get; set; }
    public bool WithdrawItem { get; set; }
}