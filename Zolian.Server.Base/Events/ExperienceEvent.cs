using Darkages.Sprites;

namespace Darkages.Events;

public struct ExperienceEvent
{
    public Aisling Player { get; }
    public int Exp { get; }
    public bool Hunting { get; }
    public bool Overflow { get; }

    public ExperienceEvent(Aisling player, int exp, bool hunting, bool overflow)
    {
        Player = player;
        Exp = exp;
        Hunting = hunting;
        Overflow = overflow;
    }
}