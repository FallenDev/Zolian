using Darkages.Sprites.Entity;

namespace Darkages.Events;

public readonly struct ExperienceEvent(Aisling player, long exp, bool hunting)
{
    public Aisling Player { get; } = player;
    public long Exp { get; } = exp;
    public bool Hunting { get; } = hunting;
}