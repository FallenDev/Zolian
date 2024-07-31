using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct BuffEvent(Sprite affected, Buff buff)
{
    public Sprite Affected { get; } = affected;
    public Buff Buff { get; } = buff;
}