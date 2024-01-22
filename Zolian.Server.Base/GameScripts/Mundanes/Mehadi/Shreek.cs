﻿using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mehadi;

[Script("Shreek")]
public class Shreek(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.HasItem("Maple Glazed Waffles") && client.Aisling.QuestManager.SwampCount == 4)
        {
            options.Add(new(0x01, "Give Waffles"));
        }

        if (client.Aisling.HasStacks("Red Onion", 10) && client.Aisling.QuestManager.SwampCount == 5)
        {
            options.Add(new(0x05, "I'm back"));
        }

        if (client.Aisling.QuestManager.SwampCount >= 5)
        {
            client.SendOptionsDialog(Mundane, "Actually, it's quite good on toast", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane, "*grumbles* Still in my swamp eh?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = Random.Shared.Next(1000000, 5000000);
        var exp2 = Random.Shared.Next(2000000, 25000000);

        switch (responseID)
        {
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x03, "Go on.."),
                        new (0x02, "Oh? Take care")
                    };

                    client.SendOptionsDialog(Mundane, "You think waffles will make me like you? For your information, there's a lot more to me than people think. I have layers!", options.ToArray());
                    break;
                }
            case 0x02:
                client.CloseDialog();
                break;
            case 0x03:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x04, "Onions, got it"),
                        new (0x02, "Nevermind")
                    };

                    client.SendOptionsDialog(Mundane, "I'll tell you what, you want in {=bMY SWAMP{=a, enter, but find me 10 Red Onions. Then I'll allow you full access.", options.ToArray());
                    break;
                }
            case 0x04:
                {
                    var item = client.Aisling.HasItemReturnItem("Maple Glazed Waffles");

                    if (item != null)
                    {
                        client.Aisling.QuestManager.SwampAccess = true;
                        client.Aisling.QuestManager.SwampCount++;
                        client.Aisling.Inventory.RemoveFromInventory(client, item);
                        client.GiveExp(exp);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
                        client.SendAttributes(StatUpdateType.WeightGold);

                        var legend = new Legend.LegendItem
                        {
                            Key = "LShreek1",
                            Time = DateTime.UtcNow,
                            Color = LegendColor.Invisible,
                            Icon = (byte)LegendIcon.Invisible,
                            Text = "Completed LShreek1"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                        client.CloseDialog();
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "WHERE ARE THEY!?");
                    }

                    break;
                }
            case 0x05:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x02, "Do you know the Muffin Man?")
                    };

                    var item = client.Aisling.HasItemReturnItem("Red Onion");

                    if (item != null)
                    {
                        client.Aisling.QuestManager.SwampCount++;
                        client.Aisling.Inventory.RemoveRange(client, item, 7);
                        client.GiveExp(exp2);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp2} experience.");
                        client.SendAttributes(StatUpdateType.WeightGold);

                        var legend = new Legend.LegendItem
                        {
                            Key = "LShreek2",
                            Time = DateTime.UtcNow,
                            Color = LegendColor.BlueG7,
                            Icon = (byte)LegendIcon.Community,
                            Text = "Gained Shreek's friendship"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "WHERE ARE THEY!?");
                    }

                    client.SendOptionsDialog(Mundane, "Huh, you actually got them for meh? I guess you're not that bad.", options.ToArray());
                    break;
                }
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}