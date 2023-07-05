using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Black Market")]
public class BlackMarket : MundaneScript
{
    public BlackMarket(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>
        {
            //new (0x01, "Dungeon Rumors"),
            new (0x02, "Buy"),
            new (0x03, "Pawn")
        };

        if (client.Aisling.IsDead())
        {
            options.Add(new OptionsDataItem(0x07, "Revive"));
        }

        client.SendOptionsDialog(Mundane, "What do you have for me?", options.ToArray());
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

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

                    var item = client.Aisling.Inventory
                        .Get(i => i != null && i.Template.Name == client.PendingItemSessions.Name).FirstOrDefault();

                    if (item != null)
                    {
                        var offer = Convert.ToString((int)(item.Template.Value / 2));

                        if (item.Stacks >= amount)
                        {
                            if (client.Aisling.GoldPoints + Convert.ToUInt32(offer) <=
                                ServerSetup.Instance.Config.MaxCarryGold)
                            {
                                client.PendingItemSessions.Offer = (uint)(Convert.ToUInt32(offer) * amount);
                                client.PendingItemSessions.Removing = amount;


                                var opts2 = new List<OptionsDataItem>
                                {
                                    new(0x0030, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                    new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                };

                                client.SendOptionsDialog(Mundane, string.Format("I can offer you {0} gold for {{=c{1} {{=q{3}s {{=a({2} Gold Each), is that a deal?", client.PendingItemSessions.Offer, amount, client.PendingItemSessions.Offer / amount, item.Template.Name), opts2.ToArray());
                            }
                        }
                        else
                        {
                            client.PendingItemSessions = null;
                            client.SendOptionsDialog(Mundane,
                                ServerSetup.Instance.Config.MerchantStackErrorMessage);
                        }
                    }
                }
            }
                break;
            case 0x02:
            {
                client.SendItemShopDialog(Mundane, "Here's what came in today.", 0x04, ShopMethods.BuyFromStoreInventory(Mundane));
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
                        item = item.Create(client.Aisling, template, ShopMethods.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());

                        if (item.GiveTo(client.Aisling))
                        {
                            client.Aisling.GoldPoints -= (uint)template.Value;
                            client.SendStats(StatusFlags.WeightMoney);
                            client.SendOptionsDialog(Mundane, $"Hope it servers you better than the last.\n{{=c{args}");
                        }
                        else
                        {
                            client.SendMessage(0x02, "Yeah right, you can't even physically hold it.");
                        }

                        break;
                    }
                    case false:
                    {
                        if (ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey("Ard Cradh"))
                        {
                            var scripts = ScriptManager.Load<SpellScript>("Ard Cradh",
                                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache["Ard Cradh"]));
                            {
                                foreach (var script in scripts.Values)
                                    script.OnUse(Mundane, client.Aisling);
                            }
                            client.SendOptionsDialog(Mundane, "Are you trying to steal from me?!");
                        }

                        break;
                    }
                }
            }
                break;
            case 0x07:
            {
                if (client.Aisling.IsDead())
                {
                    client.Recover();
                    client.TransitionToMap(3003, new Position(5, 9));
                    Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(1); });
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Always watch your six.");
                }
            }
                break;
            case 0x0500:
            {
                var itemFromSlot = client.Aisling.Inventory.Get(i => i != null).ToList().Find(i => i.InventorySlot == Convert.ToInt32(args));

                if (itemFromSlot != null)
                {
                    var offer = (int)(itemFromSlot.Template.Value / 2.4);

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
                            $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2.4x)",
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
                    var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                    if (client.Aisling.GoldPoints >= cost)
                    {
                        client.Aisling.GoldPoints -= Convert.ToUInt32(cost);
                        client.GiveQuantity(client.Aisling, item, quantity);
                        client.SendStats(StatusFlags.All);
                        client.PendingBuySessions = null;
                        client.SendOptionsDialog(Mundane, $"{{=cAlways watch your six.");
                        Task.Delay(750).ContinueWith(ct => { client.CloseDialog(); });
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Well? Where is it?");
                        client.PendingBuySessions = null;
                    }
                }
                else
                {
                    var v = args;
                    var item = client.Aisling.Inventory.Get(i => i != null && i.Template.Name == v)
                        .FirstOrDefault();

                    if (item == null)
                        return;

                    var offer = Convert.ToString((int)(item.Template.Value / 5));

                    if (Convert.ToUInt32(offer) <= 0)
                        return;

                    if (Convert.ToUInt32(offer) > item.Template.Value)
                        return;

                    if (client.Aisling.GoldPoints + Convert.ToUInt32(offer) <=
                        ServerSetup.Instance.Config.MaxCarryGold)
                    {
                        client.Aisling.GoldPoints += Convert.ToUInt32(offer);
                        client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                        client.SendStats(StatusFlags.WeightMoney);

                        client.SendOptionsDialog(Mundane, "Eh, I could have found better in the gutter.");
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
            OnResponse(client, 0x0500, item.InventorySlot.ToString());
        }
    }
}