using Darkages.Sprites;

namespace Darkages.Models;

public class EquipmentSlot(int slot, Item item)
{
    public Item Item { get; } = item;
    public int Slot { get; } = slot;
}