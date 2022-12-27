using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Callum")]
public class Callum : MundaneScript
{
    private Item _itemDetail;
    private uint _cost;

    public Callum(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        TopMenu(client);
    }

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>
        {
            new (0x0001, "Detail"),
            new (0x0002, "Nothing")
        };

        client.SendOptionsDialog(Mundane, "*cleans glass* Need something?", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        switch (responseID)
        {
            case 0x0001:
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x0003, "Let's do that."),
                    new (0x0002, "Not now.")
                };

                client.SendOptionsDialog(Mundane, "You've heard of my service then? I'm able to increase the quality of an item for a fee.", options.ToArray());
            }
                break;
            case 0x0002:
            {
                client.SendOptionsDialog(Mundane, "You know where I'll be.");
            }
                break;
            case 0x0003:
            {
                var qualitySort = ShopMethods.GetCharacterDetailingByteListForLowGradePolish(client);

                if (qualitySort.Count == 0)
                {
                    client.SendOptionsDialog(Mundane, "Your inventory currently does not have any items that I can detail for you.");
                }
                else
                {
                    client.SendItemSellDialog(Mundane, "I can polish and upgrade Damaged or Common items. Pick an item you'd like polished.", 0x0005, qualitySort);
                }
            }
                break;
            case 0x0500:
            {
                _cost = ShopMethods.GetDetailCosts(client, args);
                _itemDetail = ShopMethods.ItemDetail;

                if (_cost == 0)
                {
                    client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
                    return;
                }

                if (_itemDetail.ItemQuality is Item.Quality.Uncommon or Item.Quality.Rare or Item.Quality.Epic or Item.Quality.Legendary or Item.Quality.Forsaken)
                {
                    client.SendOptionsDialog(Mundane, "Sorry, I don't have the expertise to polish and upgrade items of this quality.");
                    return;
                }

                if (_itemDetail?.Stacks > 1 && _itemDetail.Template.CanStack)
                {
                    client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
                }
                else
                {
                    var opts2 = new List<OptionsDataItem>
                    {
                        new(0x0019, ServerSetup.Instance.Config.MerchantConfirmMessage),
                        new(0x0020, ServerSetup.Instance.Config.MerchantCancelMessage)
                    };

                    client.SendOptionsDialog(Mundane, $"It'll cost you {_cost} gold for this service.", opts2.ToArray());
                }

            }
                break;
            case 0x0019:
            {
                _itemDetail = ShopMethods.ItemDetail;

                if (client.Aisling.GoldPoints >= _cost)
                {
                    if (_itemDetail != null)
                    {
                        switch (_itemDetail.ItemQuality)
                        {
                            case Item.Quality.Damaged:
                                switch (_itemDetail.OriginalQuality)
                                {
                                    case Item.Quality.Damaged:
                                        _itemDetail.OriginalQuality = Item.Quality.Common;
                                        _itemDetail.ItemQuality = Item.Quality.Common;
                                        break;
                                    case Item.Quality.Common:
                                        _itemDetail.ItemQuality = Item.Quality.Common;
                                        break;
                                    case Item.Quality.Uncommon:
                                        _itemDetail.OriginalQuality = Item.Quality.Common;
                                        _itemDetail.ItemQuality = Item.Quality.Common;
                                        break;
                                    case Item.Quality.Rare:
                                        _itemDetail.OriginalQuality = Item.Quality.Uncommon;
                                        _itemDetail.ItemQuality = Item.Quality.Common;
                                        break;
                                    case Item.Quality.Epic:
                                        _itemDetail.OriginalQuality = Item.Quality.Rare;
                                        _itemDetail.ItemQuality = Item.Quality.Common;
                                        break;
                                    default:
                                        client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                        break;
                                }
                                break;
                            case Item.Quality.Common:
                                switch (_itemDetail.OriginalQuality)
                                {
                                    case Item.Quality.Damaged:
                                        break;
                                    case Item.Quality.Common:
                                        _itemDetail.OriginalQuality = Item.Quality.Uncommon;
                                        _itemDetail.ItemQuality = Item.Quality.Uncommon;
                                        break;
                                    case Item.Quality.Uncommon:
                                        _itemDetail.ItemQuality = Item.Quality.Uncommon;
                                        break;
                                    case Item.Quality.Rare:
                                        _itemDetail.OriginalQuality = Item.Quality.Uncommon;
                                        _itemDetail.ItemQuality = Item.Quality.Uncommon;
                                        break;
                                    case Item.Quality.Epic:
                                        _itemDetail.OriginalQuality = Item.Quality.Rare;
                                        _itemDetail.ItemQuality = Item.Quality.Uncommon;
                                        break;
                                    default:
                                        client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                        break;
                                }
                                break;
                            case Item.Quality.Uncommon:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            case Item.Quality.Rare:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            case Item.Quality.Epic:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            case Item.Quality.Legendary:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            case Item.Quality.Forsaken:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            case Item.Quality.Mythic:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                            default:
                                client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                break;
                        }
                    }

                    if (_itemDetail == null)
                    {
                        client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                        return;
                    }

                    ItemQualityVariance.ItemDurability(_itemDetail, _itemDetail.ItemQuality);
                    client.Aisling.Inventory.UpdateSlot(client, _itemDetail);
                    client.Aisling.GoldPoints -= _cost;
                    client.SendStats(StatusFlags.WeightMoney);
                    client.SendOptionsDialog(Mundane, $"I put a lot of work in your {_itemDetail?.DisplayName}{{=a, hope you like it.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "Looks like you don't have enough.");
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
        if (item == null) return;
        if (item.Template.CanStack || !item.Template.Enchantable) return;
        if (item.OriginalQuality is Item.Quality.Common or Item.Quality.Damaged)
        {
            OnResponse(client.Server, client, 0x0500, item.InventorySlot.ToString());
        }
    }
}