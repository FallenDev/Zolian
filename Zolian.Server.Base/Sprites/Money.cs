using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Types;

namespace Darkages.Sprites;

public sealed class Money : Sprite
{
    public uint Amount { get; private set; }
    public ushort Image { get; private set; }
    private MoneySprites Type { get; set; }

    private Money()
    {
        EntityType = TileContent.Money;
    }

    public static void Create(Sprite parent, uint amount, Position location)
    {
        if (parent == null) return;

        var money = new Money();
        money.CalcAmount(amount);
        money.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        var readyTime = DateTime.UtcNow;
        money.AbandonedDate = readyTime;
        money.CurrentMapId = parent.CurrentMapId;
        money.Pos = new Vector2(location.X, location.Y);
        var mt = (int)money.Type;

        if (mt > 0) money.Image = (ushort)(mt + 0x8000);

        parent.AddObject(money);
    }

    public void GiveTo(uint amount, Aisling aisling)
    {
        if (aisling.GoldPoints + amount > ServerSetup.Instance.Config.MaxCarryGold)
        {
            aisling.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Can't quite hold that much.");
            return;
        }

        aisling.GoldPoints += amount;

        if (aisling.GoldPoints > ServerSetup.Instance.Config.MaxCarryGold)
            aisling.GoldPoints = int.MaxValue;

        aisling.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've received {amount} coins.");
        aisling.Client.Send(new ServerFormat08(aisling, StatusFlags.StructC));

        Remove();
    }

    private void CalcAmount(uint amount)
    {
        Amount = amount;

        Type = Amount switch
        {
            > 0 and < 10 => MoneySprites.SilverCoin,
            >= 10 and < 100 => MoneySprites.GoldCoin,
            >= 100 and < 1000 => MoneySprites.SilverPile,
            >= 1000 and < 1000000 => MoneySprites.GoldPile,
            >= 1000000 => MoneySprites.MassGoldPile,
            _ => Type
        };
    }
}