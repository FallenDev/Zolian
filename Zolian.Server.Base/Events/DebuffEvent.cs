using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct DebuffEvent(Sprite affected, Debuff debuff)
{
    public Sprite Affected { get; } = affected;
    public Debuff Debuff { get; } = debuff;
}