using Darkages.Sprites;

namespace Darkages.Types;

public struct BuffEvent
{
    public Sprite Affected { get; }
    public Buff Buff { get; }
    public int TimeLeft { get; }

    public BuffEvent(Sprite affected, Buff buff, int timeLeft)
    {
        Affected = affected;
        Buff = buff;
        TimeLeft = timeLeft;
    }
}