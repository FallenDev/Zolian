using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites.Entity;

namespace Darkages.Events;

public readonly struct AbilityEvent(Aisling player, int exp, bool hunting) : IClientWork
{
    public Aisling Player { get; } = player;
    public int Exp { get; } = exp;
    public bool Hunting { get; } = hunting;

    public void Execute(WorldClient client) => client.ClientWorkApEvent(Player, Exp, Hunting);
}