using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.TempleOfLight;

[Script("Aerith")]
public class Aerith(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
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
            new(0x01, "Dedicate experience")
        };

        //if (client.Aisling.ExpLevel >= 250)
        //    options.Add(new Dialog.OptionsDataItem(0x02, "Dedicate ability"));

        client.SendOptionsDialog(Mundane, "Let's see if we can transpose your experience to mana.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                client.SendServerMessage(ServerMessageType.OrangeBar1, "{=cCome back again!");
                client.CloseDialog();
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, "Close your eyes, focus, now let's attempt the conversion.", options.ToArray());
                    break;
                }
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, "Close your eyes, focus, now let's attempt the conversion.", options.ToArray());
                    break;
                }
            case 0x03:
                {
                    var baseMp = client.Aisling.BaseMp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseMp >= 500000)
                        i = baseMp * 2000;
                    else
                        i = baseMp * 500;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseMp += 25;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(8, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "uhh, hmm, I'm sorry looks like you don't have enough experience.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Mana: {client.Aisling.BaseMp}", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var baseMp = client.Aisling.BaseMp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseMp >= 500000)
                        i = baseMp * 20000;
                    else
                        i = baseMp * 5000;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseMp += 250;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(1, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(8, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "uhh, hmm, I'm sorry looks like you don't have enough experience.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Mana: {client.Aisling.BaseMp}", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    var baseMp = client.Aisling.BaseMp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseMp >= 500000)
                        i = baseMp * 200000;
                    else
                        i = baseMp * 50000;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseMp += 2500;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(263, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(8, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "uhh, hmm, I'm sorry looks like you don't have enough experience.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Mana: {client.Aisling.BaseMp}", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}