using Darkages.Common;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Arena;

[Script("Inner Host")]
public class InnerHost : MundaneScript
{
    private long _repairSum;

    public InnerHost(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        TopMenu(client);
    }

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>
        {
            new(0x09, "{=qRepair All Items"),
            new(0x01, "Go North"),
            new(0x02, "Go East"),
            new(0x03, "Go South"),
            new(0x04, "Go West"),
            new(0x06, "{=cExit Arena"),
            new(0x05, "{=bThat's All for now")
        };

        if (client.Aisling.IsDead())
            options.Add(new OptionsDataItem(0x30, "{=qRevive me"));

        client.SendOptionsDialog(Mundane, "How can I help you? ", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        switch (responseID)
        {
            case 1:
            {
                var rand = Generator.RandNumGen100();

                switch (rand)
                {
                    case >= 0 and <= 24:
                        client.WarpTo(new Position(4, 4), false);
                        break;
                    case >= 25 and <= 49:
                        client.WarpTo(new Position(2, 6), false);
                        break;
                    case >= 50 and <= 74:
                        client.WarpTo(new Position(6, 2), false);
                        break;
                    case >= 75 and <= 100:
                        client.WarpTo(new Position(2, 2), false);
                        break;
                    default:
                        client.WarpTo(new Position(4, 4), false);
                        break;
                }

                client.CloseDialog();
                client.SendAnimation(262, client.Aisling, client.Aisling);
                client.SendMessage(0x03, "Northern Arena");
                break;
            }
            case 2:
            {
                var rand = Generator.RandNumGen100();

                switch (rand)
                {
                    case >= 0 and <= 24:
                        client.WarpTo(new Position(51, 4), false);
                        break;
                    case >= 25 and <= 49:
                        client.WarpTo(new Position(49, 2), false);
                        break;
                    case >= 50 and <= 74:
                        client.WarpTo(new Position(53, 6), false);
                        break;
                    case >= 75 and <= 100:
                        client.WarpTo(new Position(53, 2), false);
                        break;
                    default:
                        client.WarpTo(new Position(51, 4), false);
                        break;
                }

                client.CloseDialog();
                client.SendAnimation(262, client.Aisling, client.Aisling);
                client.SendMessage(0x03, "Eastern Arena");
                break;
            }
            case 3:
            {
                var rand = Generator.RandNumGen100();

                switch (rand)
                {
                    case >= 0 and <= 24:
                        client.WarpTo(new Position(51, 51), false);
                        break;
                    case >= 25 and <= 49:
                        client.WarpTo(new Position(49, 53), false);
                        break;
                    case >= 50 and <= 74:
                        client.WarpTo(new Position(53, 49), false);
                        break;
                    case >= 75 and <= 100:
                        client.WarpTo(new Position(53, 53), false);
                        break;
                    default:
                        client.WarpTo(new Position(51, 51), false);
                        break;
                }

                client.CloseDialog();
                client.SendAnimation(262, client.Aisling, client.Aisling);
                client.SendMessage(0x03, "Southern Arena");
                break;
            }
            case 4:
            {
                var rand = Generator.RandNumGen100();

                switch (rand)
                {
                    case >= 0 and <= 24:
                        client.WarpTo(new Position(4, 51), false);
                        break;
                    case >= 25 and <= 49:
                        client.WarpTo(new Position(2, 53), false);
                        break;
                    case >= 50 and <= 74:
                        client.WarpTo(new Position(2, 49), false);
                        break;
                    case >= 75 and <= 100:
                        client.WarpTo(new Position(6, 53), false);
                        break;
                    default:
                        client.WarpTo(new Position(4, 51), false);
                        break;
                }

                client.CloseDialog();
                client.SendAnimation(262, client.Aisling, client.Aisling);
                client.SendMessage(0x03, "Western Arena");
                break;
            }
            case 5:
            {
                client.CloseDialog();
                break;
            }
            case 6:
            {
                client.TransitionToMap(5232, new Position(3, 7));
                client.CloseDialog();
                client.SendAnimation(262, client.Aisling, client.Aisling);
                break;
            }
            case 9:
            {
                _repairSum = ShopMethods.GetRepairCosts(client);

                var optsRepair = new List<OptionsDataItem>
                {
                    new(20, ServerSetup.Instance.Config.MerchantConfirmMessage),
                    new(21, ServerSetup.Instance.Config.MerchantCancelMessage)
                };

                if (_repairSum == 0)
                {
                    client.SendOptionsDialog(Mundane, "Your items are in good condition, no repairs are necessary.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane,
                        "It will cost {=c" + _repairSum + "{=a Gold to repair everything. Do you Agree?",
                        _repairSum.ToString(), optsRepair.ToArray());
                }

                break;
            }
            case 20:
            {
                if (client.Aisling.GoldPoints >= Convert.ToUInt32(_repairSum))
                {
                    client.Aisling.GoldPoints -= Convert.ToUInt32(_repairSum);

                    client.RepairEquipment();
                    client.SendOptionsDialog(Mundane, "Here you are, fight on.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "No money, no service.");
                }
                break;
            }
            case 21:
            {
                client.SendOptionsDialog(Mundane, "Come back before anything breaks.");
                break;
            }
            case 48:
            {
                if (client.Aisling.IsDead())
                {
                    foreach (var player in client.Aisling.AislingsNearby())
                    {
                        player?.Client.SendMessage(0x05, $"{client.Aisling.Username} revived.");
                    }

                    client.Recover();
                    client.TransitionToMap(5232, new Position(3, 7));
                    client.CloseDialog();
                    Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(1); });
                }
                break;
            }
        }
    }
}