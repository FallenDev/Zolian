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
            case false when (client.Aisling.ExpLevel >= 20):
                options.Add(new(0x06, "{=qTerror of the Crypt"));
                break;
            case true when (!client.Aisling.QuestManager.CryptTerrorSlayed):
                options.Add(new(0x08, "I killed the terror?"));
                break;
        }

        switch (client.Aisling.QuestManager.CryptTerrorContinued)
        {
            case false when (client.Aisling.QuestManager.CryptTerrorSlayed && client.Aisling.ExpLevel >= 41):
                options.Add(new(0x09, "{=qContinued Terror"));
                break;
            case true when (!client.Aisling.QuestManager.CryptTerrorContSlayed):
                options.Add(new(0x0B, "I'm back again"));
                break;
        }

        switch (client.Aisling.QuestManager.NightTerror)
        {
            case false when (client.Aisling.QuestManager.CryptTerrorContSlayed && client.Aisling.ExpLevel >= 80):
                options.Add(new(0x0C, "{=qNight Terror"));
                break;
            case true when (!client.Aisling.QuestManager.NightTerrorSlayed):
                options.Add(new(0x0E, "I'm back again"));
                break;
        }

        switch (client.Aisling.QuestManager.DreamWalking)
        {
            case false when (client.Aisling.QuestManager.NightTerrorSlayed && client.Aisling.Stage >= ClassStage.Master):
                options.Add(new(0x0F, "{=qInsidious Dream Walking"));
                break;
            case true when (!client.Aisling.QuestManager.DreamWalkingSlayed):
                options.Add(new(0x12, "I believe I've ended your Terror"));
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
        var exp2 = Random.Shared.Next(540000, 810000);
        var exp3 = Random.Shared.Next(2400000, 3200000);
        var exp4 = Random.Shared.Next(10000000, 18000000);

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
                        // DrunkenHabit opens up dialog with other beggars
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
                        client.SendOptionsDialog(Mundane, "Head to {=eMileth Crypt 4-1, {=s1,19");
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
                                Key = "LBeggar1",
                                IsPublic = true,
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Yellow,
                                Icon = (byte)LegendIcon.Warrior,
                                Text = "Terror of the Crypt"
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
                case 0x09:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x0A, "I'll go."),
                            new (0x02, "No time for this.")
                        };

                        client.SendOptionsDialog(Mundane, $"I tried going back to my usual resting area, but I continue to be plagued by horrible visions. Can you investigate further?", options.ToArray());
                    }
                    break;
                case 0x0A:
                    {
                        client.Aisling.QuestManager.CryptTerrorContinued = true;
                        client.SendOptionsDialog(Mundane, "Head to {=eMileth Crypt 9-1, {=s1,45");
                    }
                    break;
                case 0x0B:
                    {
                        if (client.Aisling.HasKilled("Crypt Nightmare", 1))
                        {
                            client.Aisling.QuestManager.CryptTerrorContSlayed = true;
                            client.Aisling.QuestManager.MilethReputation += 1;
                            client.GiveExp(exp2);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp2} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            var item = new Legend.LegendItem
                            {
                                Key = "LBeggar2",
                                IsPublic = true,
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Yellow,
                                Icon = (byte)LegendIcon.Warrior,
                                Text = "Nightmare of the Crypt"
                            };

                            client.Aisling.LegendBook.AddLegend(item, client);
                            client.SendOptionsDialog(Mundane, "Finally, some peace.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "I'm still being terrified at night.",
                                new Dialog.OptionsDataItem(0x0A, "Sorry."));
                        }
                    }
                    break;
                case 0x0C:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x0D, "I'll go."),
                            new (0x02, "No time for this.")
                        };

                        client.SendOptionsDialog(Mundane, $"Oh I'm glad to see you! The dreams have gotten worse. I've not slept in days and I fear that something grotesque is causing it from deep within the crypts. Could you please go and see what can be done?", options.ToArray());
                    }
                    break;
                case 0x0D:
                    {
                        client.Aisling.QuestManager.NightTerror = true;
                        client.SendOptionsDialog(Mundane, "Head to {=eMileth Crypt 24, {=s46,1");
                    }
                    break;
                case 0x0E:
                    {
                        if (client.Aisling.HasKilled("Night Terror", 1))
                        {
                            client.Aisling.QuestManager.NightTerrorSlayed = true;
                            client.Aisling.QuestManager.MilethReputation += 1;
                            client.GiveExp(exp3);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp3} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            var item = new Legend.LegendItem
                            {
                                Key = "LBeggar3",
                                IsPublic = true,
                                Time = DateTime.UtcNow,
                                Color = LegendColor.Yellow,
                                Icon = (byte)LegendIcon.Warrior,
                                Text = "Grotesque Nightmare of the Crypt"
                            };

                            client.Aisling.LegendBook.AddLegend(item, client);
                            client.SendOptionsDialog(Mundane, "Finally, some peace.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "I'm still being terrified at night.",
                                new Dialog.OptionsDataItem(0x0D, "Sorry."));
                        }
                    }
                    break;
                case 0x0F:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x10, "What can I do?"),
                            new (0x02, "No time for this.")
                        };

                        client.SendOptionsDialog(Mundane, $"{{=aAisling! I can't take it anymore! All I can see is a {{=bRed Door {{=aevery time I close my eyes. Overwhelming terror gnaws at my very being and I fear I'm on the brink of losing what little sanity that remains.", options.ToArray());
                    }
                    break;
                case 0x10:
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new (0x11, "I'll check it out"),
                            new (0x02, "No time for this.")
                        };

                        client.SendOptionsDialog(Mundane, $"Venture to the lowest parts of the Crypt, where the Royals are laid to rest. I've heard a rumor of an ancient being, a tree. Who is well versed in all these matters. Appeal to him and please end my suffering. I beg of you!", options.ToArray());
                    }
                    break;
                case 0x11:
                    {
                        client.Aisling.QuestManager.DreamWalking = true;
                        client.SendOptionsDialog(Mundane, "Head to {=eThe Royal Crypt Mourning Room"); // 1,11
                    }
                    break;
                case 0x12:
                    {
                        if (client.Aisling.HasKilled("Dream Walker", 1) && !client.Aisling.QuestManager.DreamWalkingSlayed)
                        {
                            client.Aisling.QuestManager.DreamWalkingSlayed = true;
                            client.Aisling.QuestManager.MilethReputation += 1;
                            client.GiveExp(exp4);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp4} experience.");
                            client.SendAttributes(StatUpdateType.ExpGold);
                            var item = new Legend.LegendItem
                            {
                                Key = "LBeggar4",
                                IsPublic = true,
                                Time = DateTime.UtcNow,
                                Color = LegendColor.TurquoiseG3,
                                Icon = (byte)LegendIcon.Victory,
                                Text = "Vanquished a Dream Walker"
                            };

                            client.Aisling.LegendBook.AddLegend(item, client);
                            client.SendOptionsDialog(Mundane, "I can feel it, you did it; You actually did it!");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "I'm still being terrified at night.",
                                new Dialog.OptionsDataItem(0x11, "Sorry."));
                        }
                    }
                    break;
            }

            break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}