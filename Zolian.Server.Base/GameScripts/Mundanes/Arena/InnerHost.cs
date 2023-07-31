using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Arena;

[Script("Inner Host")]
public class InnerHost : MundaneScript
{
    private long _repairSum;

    public InnerHost(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
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
            options.Add(new Dialog.OptionsDataItem(0x30, "{=qRevive me"));

        client.SendOptionsDialog(Mundane, "How can I help you? ", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x01:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case <= 24:
                            client.WarpTo(new Position(4, 4), false);
                            break;
                        case <= 49:
                            client.WarpTo(new Position(2, 6), false);
                            break;
                        case <= 74:
                            client.WarpTo(new Position(6, 2), false);
                            break;
                        case <= 100:
                            client.WarpTo(new Position(2, 2), false);
                            break;
                        default:
                            client.WarpTo(new Position(4, 4), false);
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Northern Arena");
                    break;
                }
            case 0x02:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case <= 24:
                            client.WarpTo(new Position(51, 4), false);
                            break;
                        case <= 49:
                            client.WarpTo(new Position(49, 2), false);
                            break;
                        case <= 74:
                            client.WarpTo(new Position(53, 6), false);
                            break;
                        case <= 100:
                            client.WarpTo(new Position(53, 2), false);
                            break;
                        default:
                            client.WarpTo(new Position(51, 4), false);
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Eastern Arena");
                    break;
                }
            case 0x03:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case <= 24:
                            client.WarpTo(new Position(51, 51), false);
                            break;
                        case <= 49:
                            client.WarpTo(new Position(49, 53), false);
                            break;
                        case <= 74:
                            client.WarpTo(new Position(53, 49), false);
                            break;
                        case <= 100:
                            client.WarpTo(new Position(53, 53), false);
                            break;
                        default:
                            client.WarpTo(new Position(51, 51), false);
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Southern Arena");
                    break;
                }
            case 0x04:
                {
                    var rand = Generator.RandNumGen100();

                    switch (rand)
                    {
                        case <= 24:
                            client.WarpTo(new Position(4, 51), false);
                            break;
                        case <= 49:
                            client.WarpTo(new Position(2, 53), false);
                            break;
                        case <= 74:
                            client.WarpTo(new Position(2, 49), false);
                            break;
                        case <= 100:
                            client.WarpTo(new Position(6, 53), false);
                            break;
                        default:
                            client.WarpTo(new Position(4, 51), false);
                            break;
                    }

                    client.CloseDialog();
                    client.SendAnimation(262, client.Aisling.Serial);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Western Arena");
                    break;
                }
            case 0x05:
                {
                    client.CloseDialog();
                    break;
                }
            case 0x06:
                {
                    client.TransitionToMap(5232, new Position(3, 7));
                    client.CloseDialog();
                    client.SendAnimation(262, client.Aisling.Serial);
                    break;
                }
            case 0x09:
                {
                    _repairSum = NpcShopExtensions.GetRepairCosts(client);

                    var optsRepair = new List<Dialog.OptionsDataItem>
                    {
                        new(0x14, ServerSetup.Instance.Config.MerchantConfirmMessage),
                        new(0x15, ServerSetup.Instance.Config.MerchantCancelMessage)
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
            case 0x14:
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
            case 0x15:
                {
                    client.SendOptionsDialog(Mundane, "Come back before anything breaks.");
                    break;
                }
            case 0x30:
                {
                    if (client.Aisling.IsDead())
                    {
                        foreach (var player in client.Aisling.AislingsNearby())
                        {
                            player?.Client.SendServerMessage(ServerMessageType.OrangeBar5, $"{client.Aisling.Username} revived.");
                        }

                        client.Recover();
                        client.TransitionToMap(5232, new Position(3, 7));
                        client.CloseDialog();
                        Task.Delay(350).ContinueWith(ct =>
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, client.Aisling.Serial));
                        });
                    }
                    break;
                }
        }
    }
}