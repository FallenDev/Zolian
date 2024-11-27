﻿using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.TempleOfLight;

[Script("Cloud")]
public class Cloud(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        client.SendOptionsDialog(Mundane, "I can help you transpose your experience with your health.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                client.SendServerMessage(ServerMessageType.OrangeBar1, "{=cSee you soon");
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

                    client.SendOptionsDialog(Mundane, "Ready? Mediate and we'll convert experience to health.", options.ToArray());
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
                    var baseHp = client.Aisling.BaseHp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseHp >= 1000000)
                        i = baseHp * 2000;
                    else
                        i = baseHp * 500;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseHp += 50;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(2, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(0, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Not enough experience, sorry.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Health: {client.Aisling.BaseHp}", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var baseHp = client.Aisling.BaseHp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseHp >= 1000000)
                        i = baseHp * 20000;
                    else
                        i = baseHp * 5000;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseHp += 500;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(2, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(0, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Not enough experience, sorry.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Health: {client.Aisling.BaseHp}", options.ToArray());
                    break;
                }
            case 0x05:
                {
                    var baseHp = client.Aisling.BaseHp;
                    var baseExp = client.Aisling.ExpTotal;
                    long i;

                    if (client.Aisling.BaseHp >= 1000000)
                        i = baseHp * 200000;
                    else
                        i = baseHp * 50000;

                    if (baseExp - i >= 0)
                    {
                        client.Aisling.ExpTotal -= i;
                        client.Aisling.BaseHp += 5000;
                        client.SendAttributes(StatUpdateType.Full);
                        client.Aisling.SendAnimationNearby(170, null, client.Aisling.Serial);
                        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(0, false));
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Not enough experience, sorry.");
                        break;
                    }

                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "{=qMeditate x1"),
                        new(0x04, "{=qMeditate x10"),
                        new(0x05, "{=qMeditate x100"),
                        new(0x00, "{=bSecond thought")
                    };

                    client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Health: {client.Aisling.BaseHp}", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}