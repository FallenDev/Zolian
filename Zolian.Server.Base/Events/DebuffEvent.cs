using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public struct DebuffEvent
{
    public Sprite Affected { get; }
    public Debuff Debuff { get; }
    public TimeSpan TimeLeft { get; }

    public DebuffEvent(Sprite affected, Debuff debuff, TimeSpan timeLeft)
    {
        Affected = affected;
        Debuff = debuff;
        TimeLeft = timeLeft;
    }
}