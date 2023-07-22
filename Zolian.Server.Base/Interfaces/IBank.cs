using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

namespace Darkages.Interfaces;

public interface IBank
{
    Dictionary<uint, Item> Items { get; }
    Task<bool> Deposit(WorldClient client, Item item);
    void AddToAislingDb(Aisling aisling, Item item);
    Task UpdateBanked(Aisling aisling, Item item);
    Task<bool> Withdraw(WorldClient client, Mundane mundane);
    void DeleteFromAislingDb(IWorldClient client);
    void DepositGold(IWorldClient client, uint gold);
    void WithdrawGold(IWorldClient client, uint gold);
    void UpdatePlayersWeight(WorldClient client);
}