using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

using System.Collections.Concurrent;

namespace Darkages.Interfaces;

public interface IBank
{
    ConcurrentDictionary<long, Item> Items { get; }
    public ulong TempGoldDeposit { get; set; }
    public ulong TempGoldWithdraw { get; set; }
    void DepositGold(IWorldClient client, ulong gold);
    void WithdrawGold(IWorldClient client, ulong gold);
}