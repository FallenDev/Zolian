using Darkages.Sprites;

namespace Darkages.Events;

public readonly struct ItemEvent(Item item)
{
    public Item Item { get; } = item;
}