using System.Collections.Concurrent;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

namespace Darkages.Interfaces;

public interface IBank
{
    ConcurrentDictionary<uint, Item> Items { get; }
    void DepositGold(IWorldClient client, uint gold);
    void WithdrawGold(IWorldClient client, uint gold);
}