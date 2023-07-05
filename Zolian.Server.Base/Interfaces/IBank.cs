using Darkages.Network.Client;
using Darkages.Sprites;

namespace Darkages.Interfaces;

public interface IBank
{
    Dictionary<uint, Item> Items { get; }
    Task<bool> Deposit(GameClient client, Item item);
    void AddToAislingDb(ISprite aisling, Item item);
    Task UpdateBanked(ISprite aisling, Item item);
    Task<bool> Withdraw(GameClient client, Mundane mundane);
    void DeleteFromAislingDb(IGameClient client);
    void DepositGold(IGameClient client, uint gold);
    void WithdrawGold(IGameClient client, uint gold);
    void UpdatePlayersWeight(GameClient client);
}