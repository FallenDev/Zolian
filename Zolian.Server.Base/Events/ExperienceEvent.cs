using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites.Entity;

namespace Darkages.Events;

public readonly struct ExperienceEvent(Aisling player, long exp, bool hunting) : IClientWork
{
    public Aisling Player { get; } = player;
    public long Exp { get; } = exp;
    public bool Hunting { get; } = hunting;

    public void Execute(WorldClient client) => client.ClientWorkExpEvent(Player, Exp, Hunting);
}