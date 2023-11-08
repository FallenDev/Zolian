using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Senan")]
public class Senan(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        switch (client.Aisling.QuestManager.CryptTerror)
        {
            case false when (client.Aisling.Level >= 20):
                options.Add(new(0x06, "{=qTerror of the Crypt"));
                break;
            case true when (!client.Aisling.QuestManager.CryptTerrorSlayed):
                options.Add(new(0x08, "I killed the terror?"));
                break;
        }

        options.Add(new(0x02, "No."));

        if (!client.Aisling.QuestManager.DrunkenHabit)
            options.Add(new(0x03, "Sure."));

        client.SendOptionsDialog(Mundane, "Would you kind sir, spare some change?", options.ToArray());
    }

    public override void OnGoldDropped(WorldClient client, uint money)
    {
        client.SendOptionsDialog(Mundane,
            money >= 1 ? "Wow, thank you. *stumbles to bar to order more alcohol*" : "I'm sorry, where is it?");
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = Random.Shared.Next(200000, 550000);

        while (true)
        {
            var money = args;

            switch (responseID)
            {
                case 0x01:
                    {
                        if (money != null)
                        {
                            uint.TryParse(money, out var donation);
                            if (client.Aisling.GoldPoints >= donation)
                                client.Aisling.GoldPoints -= donation;

                            if (donation <= 1000)
                            {
                                responseID = 0x02;
                                args = null;
                                continue;
                            }

                            responseID = 0x04;
                            args = null;
                            continue;
                        }
                    }
                    break;
                case 0x02:
                    {
                        client.SendOptionsDialog(Mundane, "Hic... Damn");
                        client.SendAttributes(StatUpdateType.WeightGold);
                    }
                    break;
                case 0x03:
                    {
                        client.SendTextInput(Mundane, "How much would you like to give?", "I'm broke.", 6);
                    }
                    break;
                case 0x04:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x05, "Thank you.")
                        };

                        client.SendOptionsDialog(Mundane, $"Wow, I never thought I'd meet someone so generous.\n *mutters a moment*; I have something for you.   Hic!", options.ToArray());
                    }
                    break;
                case 0x05:
                    {
                        client.Aisling.QuestManager.DrunkenHabit = true;
                        client.Aisling.QuestManager.MilethReputation += 1;
                        client.GiveItem("Mold");
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.CloseDialog();
                    }
                    break;
                case 0x06:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x07, "I'll go."),
                            new (0x02, "No time for this.")
                        };

                        client.SendOptionsDialog(Mundane, $"The crypt is usually where I sleep, recently something has been terrifying me. Can you have a look?", options.ToArray());
                    }
                    break;
                case 0x07:
                    {
                        client.Aisling.QuestManager.CryptTerror = true;
                        client.SendOptionsDialog(Mundane, "Head to {=eMileth Crypt 4-1, {=s1,19.");
                    }
                    break;
                case 0x08:
                    {
                        if (client.Aisling.HasKilled("Crypt Terror", 1))
                        {
                            client.Aisling.QuestManager.CryptTerrorSlayed = true;
                            client.Aisling.QuestManager.MilethReputation += 1;
                            client.GiveExp(exp);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            var item = new Legend.LegendItem
                            {
                                Category = "LBeggar1",
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Yellow,
                                Icon = (byte)LegendIcon.Warrior,
                                Value = "Terror of the Crypt"
                            };

                            client.Aisling.LegendBook.AddLegend(item, client);

                            client.SendOptionsDialog(Mundane, "Finally, some peace.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "I'm still being terrified at night.",
                                new Dialog.OptionsDataItem(0x07, "Sorry."));
                        }
                    }
                    break;
            }

            break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}