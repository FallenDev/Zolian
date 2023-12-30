using Chaos.Common.Definitions;
using Darkages.Interfaces;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using System.Collections.Concurrent;

namespace Darkages.Managers;

public class BankManager : IBank
{
    public ConcurrentDictionary<long, Item> Items { get; } = new();
    public ulong TempGoldDeposit { get; set; }
    public ulong TempGoldWithdraw { get; set; }

    public void DepositGold(IWorldClient client, ulong gold)
    {
        client.Aisling.GoldPoints -= gold;
        client.Aisling.BankedGold += gold;
        client.SendAttributes(StatUpdateType.ExpGold);
    }

    public void WithdrawGold(IWorldClient client, ulong gold)
    {
        client.Aisling.GoldPoints += gold;
        client.Aisling.BankedGold -= gold;
        client.SendAttributes(StatUpdateType.ExpGold);
    }
}