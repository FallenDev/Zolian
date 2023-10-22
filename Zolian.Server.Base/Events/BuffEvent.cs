using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct BuffEvent(Sprite affected, Buff buff, TimeSpan timeLeft)
{
    public Sprite Affected { get; } = affected;
    public Buff Buff { get; } = buff;
    public TimeSpan TimeLeft { get; } = timeLeft;
}