using Darkages.Sprites;

namespace Darkages.Types;

public struct DebuffEvent
{
    public Sprite Affected { get; }
    public Debuff Debuff { get; }
    public int TimeLeft { get; }

    public DebuffEvent(Sprite affected, Debuff debuff, int timeLeft)
    {
        Affected = affected;
        Debuff = debuff;
        TimeLeft = timeLeft;
    }
}