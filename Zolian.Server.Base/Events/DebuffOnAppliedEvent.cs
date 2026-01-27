using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Events;

public readonly struct DebuffOnAppliedEvent(Sprite affected, Debuff debuff) : IClientWork
{
    public Sprite Affected { get; } = affected;
    public Debuff Debuff { get; } = debuff;

    public void Execute(WorldClient client) => client.ClientWorkDebuffAppliedEvent(Affected, Debuff);
}