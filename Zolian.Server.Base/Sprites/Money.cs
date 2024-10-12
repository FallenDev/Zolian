using Chaos.Common.Identity;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.Types;

using System.Numerics;

namespace Darkages.Sprites;

public sealed class Money : Sprite
{
    public long MoneyId { get; private set; }
    private ulong Amount { get; set; }
    public ushort Image { get; private set; }
    private MoneySprites Type { get; set; }

    private Money()
    {
        TileType = TileContent.Money;
    }

    public static void Create(Sprite parent, ulong amount, Position location)
    {
        if (parent == null) return;

        var money = new Money();
        money.CalcAmount(amount);
        money.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        money.MoneyId = EphemeralRandomIdGenerator<long>.Shared.NextId;
        var readyTime = DateTime.UtcNow;
        money.AbandonedDate = readyTime;
        money.CurrentMapId = parent.CurrentMapId;
        money.Pos = new Vector2(location.X, location.Y);
        var mt = (int)money.Type;

        if (mt > 0) money.Image = (ushort)mt;

        AddObject(money);
        ServerSetup.Instance.GlobalGroundMoneyCache.TryAdd(money.MoneyId, money);
    }

    public static void GiveTo(Money money, Aisling aisling)
    {
        var amount = money.Amount;
        if (aisling.GoldPoints + amount > ServerSetup.Instance.Config.MaxCarryGold)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Can't quite hold that much.");
            return;
        }
        
        aisling.GoldPoints += amount;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've received {amount} coins.");
        aisling.Client.SendAttributes(StatUpdateType.ExpGold);

        var removed = ServerSetup.Instance.GlobalGroundMoneyCache.TryRemove(money.MoneyId, out var itemToBeRemoved);
        if (!removed) return;
        itemToBeRemoved.Remove();
    }

    private void CalcAmount(ulong amount)
    {
        Amount = amount;

        Type = Amount switch
        {
            > 0 and < 10 => MoneySprites.CopperCoin,
            >= 10 and < 100 => MoneySprites.CopperPile,
            >= 100 and < 500 => MoneySprites.SilverCoin,
            >= 500 and < 1000 => MoneySprites.SilverPile,
            >= 1000 and < 50000 => MoneySprites.GoldCoin,
            >= 50000 and < 1000000 => MoneySprites.GoldPile,
            >= 1000000 => MoneySprites.MassGoldPile,
            _ => Type
        };
    }
}