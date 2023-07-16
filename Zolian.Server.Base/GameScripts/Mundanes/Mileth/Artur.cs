using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Artur's Gift")]
public class Artur : MundaneScript
{
    public Artur(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Level == 99 && client.Aisling.QuestManager.ArtursGift == 0)
        {
            var options = new List<Dialog.OptionsDataItem>
            {
                new (0x02, "Yes?"),
                new (0x03, "Ok...")
            };

            client.SendOptionsDialog(Mundane, "You look more than capable.. but I wonder. {=c(({=qCompleting this quest will prevent you from subbing{=c.))", options.ToArray());
        }
        else switch (client.Aisling.QuestManager.ArtursGift)
        {
            case 1:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new(0x06, "I found a strange lexicon."),
                    new(0x07, "I haven't found one yet..")
                };
                    
                client.SendOptionsDialog(Mundane, "So you've found a replica of Chadul's lexicon? Let me have a look.", options.ToArray());
                break;
            }
            case 2:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new(0x08, "So what do you have?")
                };

                client.SendOptionsDialog(Mundane, "One moment. ~Ard Naomh Sgiath Spion Meas~!", options.ToArray());
                break;
            }
            case 3:
            {
                client.SendOptionsDialog(Mundane, "Proceed to the {=qHall of Souls {=anear the edge of town.");
                break;
            }
            default:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x03, "Alright.")
                };

                client.SendOptionsDialog(Mundane, "You lack strength nor have the insight desired.", options.ToArray());
                break;
            }
        }

    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 2:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x04, "I'll try my best."),
                    new (0x05, "What do I look like, your dog?"),
                    new (0x03, "No.")
                };

                client.SendOptionsDialog(Mundane, "As you're aware, when the gods lost their powers; Chadul's servants placed their lexicons deep within the Tagor crypts. If you could bring me back one, I'll make it worth your effort.", options.ToArray());
                break;
            }
            case 3:
                client.CloseDialog();
                break;
            case 4:
            {
                client.Aisling.QuestManager.ArtursGift = 1;
                client.SendOptionsDialog(Mundane, $"Thank you, {client.Aisling.Username}.");
                break;
            }
            case 5:
            {
                var debuff = new debuff_ardcradh();
                debuff.OnApplied(client.Aisling, debuff);
                client.SendOptionsDialog(Mundane, "Scram or you will be.");
                break;
            }
            case 6:
            {
                switch (client.Aisling.Path)
                {
                    case Class.Berserker:
                    {
                        if (client.Aisling.HasItem("Chadul's Berserker Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Berserker Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    case Class.Defender:
                    {
                        if (client.Aisling.HasItem("Chadul's Defender Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Defender Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    case Class.Assassin:
                    {
                        if (client.Aisling.HasItem("Chadul's Assassin Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Assassin Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    case Class.Cleric:
                    {
                        if (client.Aisling.HasItem("Chadul's Cleric Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Cleric Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    case Class.Arcanus:
                    {
                        if (client.Aisling.HasItem("Chadul's Arcanus Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Arcanus Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    case Class.Monk:
                    {
                        if (client.Aisling.HasItem("Chadul's Monk Lexicon"))
                        {
                            var item = client.Aisling.HasItemReturnItem("Chadul's Monk Lexicon");
                            if (item == null) TopMenu(client);
                            client.Aisling.QuestManager.ArtursGift = 2;
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.GiveExp(3500000);
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 3,500,000 experience.");
                            client.SendAttributes(StatUpdateType.WeightGold);
                            client.SendOptionsDialog(Mundane,
                                "Great, give me a moment and I'll have something for you.");
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane,
                                "Wrong lexicon, come back with the one for your path.");
                        }

                        break;
                    }
                    default:
                        client.CloseDialog();
                        break;
                }

                break;
            }
            case 7:
            {
                client.SendOptionsDialog(Mundane, "Come back when you have, they're located in {=bTagor{=a'{=bs Crypt{=a.");
                break;
            }
            case 8:
            {
                switch (client.Aisling.Path)
                {
                    case Class.Berserker:
                    {
                        client.GiveItem("Ceannlaidir's Tamed Sword");
                        if (client.Aisling.HasItem("Ceannlaidir's Tamed Sword"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    case Class.Defender:
                    {
                        client.GiveItem("Ceannlaidir's Enchanted Sword");
                        if (client.Aisling.HasItem("Ceannlaidir's Enchanted Sword"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    case Class.Assassin:
                    {
                        client.GiveItem("Fiosachd's Lost Flute");
                        if (client.Aisling.HasItem("Fiosachd's Lost Flute"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    case Class.Cleric:
                    {
                        client.GiveItem("Glioca's Secret");
                        if (client.Aisling.HasItem("Glioca's Secret"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    case Class.Arcanus:
                    {
                        client.GiveItem("Luathas's Lost Relic");
                        if (client.Aisling.HasItem("Luathas's Lost Relic"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    case Class.Monk:
                    {
                        client.GiveItem("Cail's Hourglass");
                        if (client.Aisling.HasItem("Cail's Hourglass"))
                        {
                            client.Aisling.QuestManager.ArtursGift = 3;
                            client.Aisling.QuestManager.MilethReputation += 1;
                        }
                        break;
                    }
                    default:
                        client.CloseDialog();
                        break;
                }

                client.SendOptionsDialog(Mundane, "This aisling is what's called a master's artifact. With this item now infused with the lexicon you may begin your journey towards mastership. Before aislings would need to find relics deep within Cthonic Realms. With Chadul's curse those relics are now useless. Aisling, take this item to the {=qHall of Souls.");
                break;
            }
        }
    }
}