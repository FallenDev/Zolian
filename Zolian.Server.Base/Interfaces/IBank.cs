using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

using System.Collections.Concurrent;

namespace Darkages.Interfaces;

public interface IBank
{
    ConcurrentDictionary<long, Item> Items { get; }
    public long TempGoldDeposit { get; set; }
    public long TempGoldWithdraw { get; set; }
    void DepositGold(IWorldClient client, long gold);
    void WithdrawGold(IWorldClient client, long gold);
}