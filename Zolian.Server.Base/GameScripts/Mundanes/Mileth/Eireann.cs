using Darkages.Enums;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mileth
{
    [Script("Eireann")]
    public class Eireann : MundaneScript
    {
        public Eireann(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            var options = new List<OptionsDataItem>();

            switch (client.Aisling.QuestManager.EternalLove)
            {
                case false when (client.Aisling.Level >= 50):
                    options.Add(new(0x06, "{=qEternal Love"));
                    break;
                case true when (!client.Aisling.QuestManager.CryptTerrorSlayed):
                    options.Add(new(0x08, "..."));
                    break;
            }

            options.Add(new(0x01, "Rumors"));
            options.Add(new(0x02, "Buy"));
            options.Add(new(0x03, "Pawn"));

            client.SendOptionsDialog(Mundane, "Greetings Adventurer, care for some mead?", options.ToArray());
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            if (client.Aisling.Map.ID != Mundane.Map.ID)
            {
                client.Dispose();
                return;
            }

            var gossip = Random.Shared.Next(1, 6);

            switch (responseID)
            {
                case 0x0000:
                    {
                        if (string.IsNullOrEmpty(args)) return;

                        int.TryParse(args, out var amount);

                        if (amount > 0 && client.PendingBuySessions != null)
                        {
                            client.PendingBuySessions.Quantity = amount;
                            var item = client.PendingBuySessions.Name;

                            if (item != null)
                            {
                                var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                                var opts = new List<OptionsDataItem>
                                {
                                    new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                    new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                };

                                client.SendOptionsDialog(Mundane, $"It will cost you a total of {cost} coins for {{=c{amount} {{=q{item}s{{=a. Is that a deal?", opts.ToArray());
                            }
                        }

                        if (amount > 0 && client.PendingItemSessions != null)
                        {
                            client.PendingItemSessions.Quantity = amount;

                            var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

                            if (item != null)
                            {
                                var offer = (int)(item.Template.Value / 2);

                                if (item.Stacks >= amount)
                                {
                                    if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                                    {
                                        client.PendingItemSessions.Offer = (uint)(offer * amount);
                                        client.PendingItemSessions.Removing = amount;

                                        var opts2 = new List<OptionsDataItem>
                                        {
                                            new(0x0030, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                            new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                        };

                                        client.SendOptionsDialog(Mundane, string.Format("I can offer you {0} gold for {{=c{1} {{=q{3}s {{=a({2} Gold Each), is that a deal?",
                                                client.PendingItemSessions.Offer, amount, client.PendingItemSessions.Offer / amount, item.Template.Name), opts2.ToArray());
                                    }
                                    else
                                    {
                                        client.PendingItemSessions = null;
                                        client.SendOptionsDialog(Mundane, "Looks like you can't carry anymore gold. Why don't you buy something?");
                                    }
                                }
                                else
                                {
                                    client.PendingItemSessions = null;
                                    client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantStackErrorMessage);
                                }
                            }
                        }
                    }
                    break;
                case 0x01:
                    {
                        switch (gossip)
                        {
                            case 1:
                                client.SendOptionsDialog(Mundane, "You can recover from wounds by laying down (Clicking a {=qBed{=a).");
                                break;
                            case 2:
                                client.SendOptionsDialog(Mundane, "Not all quests offer rewards that you can see, some times it's good to just help people.");
                                break;
                            case 3:
                                client.SendOptionsDialog(Mundane, "I overheard that there is a secret shop in Abel... I wonder where?");
                                // ToDo: Make sure this is enabled for aislings other than assassins on entry
                                client.Aisling.QuestManager.AbelShopAccess = true;
                                break;
                            case 4:
                                client.SendOptionsDialog(Mundane, "There are three basement access panels to the crypts.");
                                break;
                            case 5:
                                client.SendOptionsDialog(Mundane, "I heard a rumor that the altar can give you {=bForsaken{=a items if you're really lucky.");
                                break;
                        }
                    }
                    break;
                case 0x02:
                    {
                        client.SendItemShopDialog(Mundane, "Only the finest.", 0x04, ShopMethods.BuyFromStoreInventory(Mundane));
                    }
                    break;
                case 0x03:
                    {
                        client.SendItemSellDialog(Mundane, "What do you want to pawn?", 0x0005, ShopMethods.GetCharacterSellInventoryByteList(client));
                    }
                    break;
                case 0x04:
                    {
                        if (string.IsNullOrEmpty(args)) return;
                        if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(args)) return;

                        var template = ServerSetup.Instance.GlobalItemTemplateCache[args];

                        switch (template.CanStack)
                        {
                            case true:
                                client.PendingBuySessions = new PendingBuy
                                {
                                    Name = template.Name,
                                    Quantity = 0,
                                    Offer = (int)template.Value,
                                };
                                client.Send(new ServerFormat2F(Mundane, $"How many {template.Name} would you like to purchase?", new TextInputData()));
                                break;
                            case false when client.Aisling.GoldPoints >= template.Value:
                                {
                                    var item = new Item();
                                    item = item.Create(client.Aisling, template, Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

                                    if (item.GiveTo(client.Aisling))
                                    {
                                        client.Aisling.GoldPoints -= template.Value;

                                        client.SendStats(StatusFlags.WeightMoney);
                                        client.SendOptionsDialog(Mundane, $"*slides* your {{=c{args} {{=aover to you.");
                                    }
                                    else
                                    {
                                        client.SendMessage(0x02, "Yeah right, you're too drunk.");
                                    }

                                    break;
                                }
                            case false:
                                {
                                    client.SendOptionsDialog(Mundane, "You're a mess, I'm cutting you off!");
                                    break;
                                }
                        }
                    }
                    break;
                case 0x0500:
                    {
                        var itemFromSlot = client.Aisling.Inventory.Get(i => i != null).ToList().Find(i => i.InventorySlot == Convert.ToInt32(args));

                        if (itemFromSlot != null)
                        {
                            var offer = (int)(itemFromSlot.Template.Value / 1.2);

                            if (offer <= 0)
                            {
                                client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantRefuseTradeMessage);
                                return;
                            }

                            if (itemFromSlot.Stacks > 1 && itemFromSlot.Template.CanStack)
                            {
                                client.PendingItemSessions = new PendingSell
                                {
                                    ID = itemFromSlot.ItemId,
                                    Name = itemFromSlot.Template.Name,
                                    Quantity = 0
                                };

                                client.Send(new ServerFormat2F(Mundane,
                                    $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}",
                                    new TextInputData()));
                            }
                            else
                            {
                                client.PendingItemSessions = new PendingSell
                                {
                                    ID = itemFromSlot.ItemId,
                                    Name = itemFromSlot.Template.Name,
                                    Quantity = 1
                                };

                                var opts2 = new List<OptionsDataItem>
                                {
                                    new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                    new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                };

                                client.SendOptionsDialog(Mundane,
                                    $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (1.2x)",
                                    itemFromSlot.Template.Name, opts2.ToArray());
                            }
                        }
                    }
                    break;
                case 0x0030:
                    {
                        if (client.PendingItemSessions != null)
                        {
                            ShopMethods.CompletePendingItemSell(client);
                        }

                        TopMenu(client);
                    }
                    break;
                case 0x0019:
                    {
                        if (client.PendingBuySessions != null)
                        {
                            var quantity = client.PendingBuySessions.Quantity;
                            var item = client.PendingBuySessions.Name;
                            var cost = (uint)(client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity);
                            if (client.Aisling.GoldPoints >= cost)
                            {
                                client.Aisling.GoldPoints -= cost;
                                client.GiveQuantity(client.Aisling, item, quantity);
                                client.SendStats(StatusFlags.WeightMoney);
                                client.PendingBuySessions = null;
                                client.SendMessage(0x03, $"{{=cBottoms Up!");
                                TopMenu(client);
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Well? Where is it?");
                                client.PendingBuySessions = null;
                            }
                        }

                        if (client.PendingItemSessions != null)
                        {
                            var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

                            if (item == null) return;

                            var offer = (uint)(item.Template.Value / 2.8);

                            if (offer <= 0) return;
                            if (offer > item.Template.Value) return;

                            if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                            {
                                client.Aisling.GoldPoints += offer;
                                client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                                client.SendStats(StatusFlags.WeightMoney);
                                client.PendingItemSessions = null;
                                client.SendMessage(0x03, $"{{=cSee you around.");
                                TopMenu(client);
                            }
                        }
                    }
                    break;
                case 0x0020:
                    {
                        client.PendingItemSessions = null;
                        client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
                    }
                    break;
            }
        }

        public override void OnItemDropped(GameClient client, Item item)
        {
            if (client == null) return;
            if (item == null) return;
            if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
            {
                OnResponse(client.Server, client, 0x0500, item.InventorySlot.ToString());
            }
        }
    }
}