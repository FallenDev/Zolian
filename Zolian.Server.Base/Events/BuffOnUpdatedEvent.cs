using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct BuffOnUpdatedEvent(Sprite affected, Buff buff) : IClientWork
{
    public Sprite Affected { get; } = affected;
    public Buff Buff { get; } = buff;

    public void Execute(WorldClient client) => client.ClientWorkBuffUpdatedEvent(Affected, Buff);
}