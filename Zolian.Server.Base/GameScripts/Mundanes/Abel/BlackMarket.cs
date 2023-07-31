using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Black Market")]
public class BlackMarket : MundaneScript
{
    public BlackMarket(WorldServer server, Mundane mundane) : base(server, mundane) { }

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
            //new (0x01, "Dungeon Rumors"),
            new (0x02, "Buy"),
            new (0x03, "Pawn")
        };

        if (client.Aisling.IsDead())
        {
            options.Add(new Dialog.OptionsDataItem(0x07, "Revive"));
        }

        client.SendOptionsDialog(Mundane, "What do you have for me?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x0000:
                {
                    if (string.IsNullOrEmpty(args)) return;

                    var itemOrSlot = ushort.TryParse(args, out var amountOrSlot);

                    switch (itemOrSlot)
                    {
                        case false:
                            {
                                // ToDo: Add in reactor to buy multiple if a stackable item
                                amountOrSlot = 1;

                                ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(args, out var itemTemplate);
                                if (itemTemplate == null)
                                {
                                    client.SendOptionsDialog(Mundane, "I'm sorry, freshly sold out.");
                                    return;
                                }

                                client.PendingBuySessions = new PendingBuy
                                {
                                    Name = itemTemplate.Name,
                                    Offer = (int)itemTemplate.Value,
                                    Quantity = 1
                                };

                                var itemName = client.PendingBuySessions.Name;

                                if (itemName != null)
                                {
                                    var cost = client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity;
                                    var opts = new List<Dialog.OptionsDataItem>
                                    {
                                        new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                        new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                    };

                                    client.SendOptionsDialog(Mundane, $"It will cost you a total of {cost} coins for {{=c{amountOrSlot} {{=q{itemName}s{{=a. Is that a deal?", opts.ToArray());
                                }

                                break;
                            }
                        case true when amountOrSlot > 0:
                            {
                                client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(args), out var itemFromSlot);

                                if (itemFromSlot == null)
                                {
                                    client.SendOptionsDialog(Mundane, "Sorry, what was that again?");
                                    return;
                                }

                                var offer = (int)(itemFromSlot.Template.Value / 2);

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

                                    client.SendTextInput(Mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}");
                                }
                                else
                                {
                                    client.PendingItemSessions = new PendingSell
                                    {
                                        ID = itemFromSlot.ItemId,
                                        Name = itemFromSlot.Template.Name,
                                        Quantity = 1
                                    };

                                    var opts2 = new List<Dialog.OptionsDataItem>
                                    {
                                        new(0x0030, ServerSetup.Instance.Config.MerchantConfirmMessage),
                                        new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                                    };

                                    client.SendOptionsDialog(Mundane,
                                        $"I can offer you {offer} gold for that {{=q{itemFromSlot.DisplayName}{{=a, is that a deal? (2x)",
                                        itemFromSlot.Template.Name, opts2.ToArray());
                                }

                                break;
                            }
                    }
                }
                break;
            case 0x02:
                {
                    client.SendItemShopDialog(Mundane, "Here's what came in today.", 0x04, NpcShopExtensions.BuyFromStoreInventory(Mundane));
                }
                break;
            case 0x03:
                {
                    client.SendItemSellDialog(Mundane, "What do you want to pawn?", 0x0005, NpcShopExtensions.GetCharacterSellInventoryByteList(client));
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
                            client.SendTextInput(Mundane, $"How many {template.Name} would you like to purchase?");
                            break;
                        case false when client.Aisling.GoldPoints >= template.Value:
                            {
                                var item = new Item();
                                item = item.Create(client.Aisling, template, NpcShopExtensions.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());

                                if (item.GiveTo(client.Aisling))
                                {
                                    client.Aisling.GoldPoints -= (uint)template.Value;
                                    client.SendAttributes(StatUpdateType.WeightGold);
                                    client.SendOptionsDialog(Mundane, $"Hope it servers you better than the last.\n{{=c{args}");
                                }
                                else
                                {
                                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Yeah right, you can't even physically hold it.");
                                }

                                break;
                            }
                        case false:
                            {
                                if (ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue("Ard Cradh", out var value))
                                {
                                    var scripts = ScriptManager.Load<SpellScript>("Ard Cradh",
                                        Spell.Create(1, value));
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
                        Task.Delay(350).ContinueWith(ct => { client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, client.Aisling.Serial)); });
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Always watch your six.");
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

                            client.SendTextInput(Mundane, $"How many {{=q{itemFromSlot.Template.Name} {{=awould you like to sell?\nStack Size: {itemFromSlot.Stacks}");
                        }
                        else
                        {
                            client.PendingItemSessions = new PendingSell
                            {
                                ID = itemFromSlot.ItemId,
                                Name = itemFromSlot.Template.Name,
                                Quantity = 1
                            };

                            var opts2 = new List<Dialog.OptionsDataItem>
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
                        NpcShopExtensions.CompletePendingItemSell(client, Mundane);
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
                            client.SendAttributes(StatUpdateType.Full);
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
                            client.Aisling.Inventory.RemoveFromInventory(client, item);
                            client.SendAttributes(StatUpdateType.WeightGold);

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

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
        {
            OnResponse(client, 0x0500, item.InventorySlot.ToString());
        }
    }
}