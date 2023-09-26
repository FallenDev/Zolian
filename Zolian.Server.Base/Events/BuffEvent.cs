using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public struct BuffEvent
{
    public Sprite Affected { get; }
    public Buff Buff { get; }
    public TimeSpan TimeLeft { get; }

    public BuffEvent(Sprite affected, Buff buff, TimeSpan timeLeft)
    {
        Affected = affected;
        Buff = buff;
        TimeLeft = timeLeft;
    }
}