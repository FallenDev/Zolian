using Darkages.Network.Client;
using Darkages.Sprites;

namespace Darkages.Interfaces;

public interface IBank
{
    Dictionary<uint, Item> Items { get; }
    Task<bool> Deposit(WorldClient client, Item item);
    void AddToAislingDb(ISprite aisling, Item item);
    Task UpdateBanked(ISprite aisling, Item item);
    Task<bool> Withdraw(WorldClient client, Mundane mundane);
    void DeleteFromAislingDb(WorldClient client);
    void DepositGold(WorldClient client, uint gold);
    void WithdrawGold(WorldClient client, uint gold);
    void UpdatePlayersWeight(WorldClient client);
}