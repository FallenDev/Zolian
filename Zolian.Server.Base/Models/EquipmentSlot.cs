using Darkages.Sprites;

namespace Darkages.Models;

public class EquipmentSlot
{
    public EquipmentSlot(int slot, Item item)
    {
        Slot = slot;
        Item = item;
    }

    public Item Item { get; }
    public int Slot { get; }
}