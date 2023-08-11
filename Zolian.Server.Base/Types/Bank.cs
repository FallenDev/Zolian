﻿using System.Collections.Concurrent;
using Chaos.Common.Definitions;
using Darkages.Interfaces;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

namespace Darkages.Types;

public class Bank : IBank
{
    public Bank()
    {
        Items = new ConcurrentDictionary<uint, Item>();
    }

    public ConcurrentDictionary<uint, Item> Items { get; }
    public long TempGoldDeposit { get; set; }
    public long TempGoldWithdraw { get; set; }

    public void DepositGold(IWorldClient client, long gold)
    {
        client.Aisling.GoldPoints -= gold;
        client.Aisling.BankedGold += gold;
        client.SendAttributes(StatUpdateType.ExpGold);
    }

    public void WithdrawGold(IWorldClient client, long gold)
    {
        client.Aisling.GoldPoints += gold;
        client.Aisling.BankedGold -= gold;
        client.SendAttributes(StatUpdateType.ExpGold);
    }
}