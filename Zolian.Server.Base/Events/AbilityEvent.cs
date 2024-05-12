using Darkages.Sprites;

namespace Darkages.Events;

public readonly struct AbilityEvent(Aisling player, int exp, bool hunting)
{
    public Aisling Player { get; } = player;
    public int Exp { get; } = exp;
    public bool Hunting { get; } = hunting;
}