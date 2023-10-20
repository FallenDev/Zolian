using Darkages.Sprites;

namespace Darkages.Events;

public struct AbilityEvent(Aisling player, int exp, bool hunting, bool overflow)
{
    public Aisling Player { get; } = player;
    public int Exp { get; } = exp;
    public bool Hunting { get; } = hunting;
    public bool Overflow { get; } = overflow;
}