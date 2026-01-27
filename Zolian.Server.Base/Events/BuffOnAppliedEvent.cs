using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct BuffOnAppliedEvent(Sprite affected, Buff buff) : IClientWork
{
    public Sprite Affected { get; } = affected;
    public Buff Buff { get; } = buff;

    public void Execute(WorldClient client) => client.ClientWorkBuffAppliedEvent(Affected, Buff);
}