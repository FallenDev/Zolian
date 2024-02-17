using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Undine;

[Script("Ameen")]
public class Ameen(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private Item _itemDetail;
    private uint _cost;

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
            new (0x01, "Detail"),
            new (0x02, "Nothing")
        };

        client.SendOptionsDialog(Mundane, "*cleans glass* Need something?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                {
                    _cost = NpcShopExtensions.GetDetailCosts(client, args);
                    _itemDetail = NpcShopExtensions.ItemDetail;

                    if (_cost == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Yea, I won't touch that.");
                        return;
                    }

                    if (_itemDetail.ItemQuality is Item.Quality.Rare or Item.Quality.Epic or Item.Quality.Legendary or Item.Quality.Forsaken)
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
                        var opts2 = new List<Dialog.OptionsDataItem>
                        {
                            new(0x19, ServerSetup.Instance.Config.MerchantConfirmMessage),
                            new(0x20, ServerSetup.Instance.Config.MerchantCancelMessage)
                        };

                        client.SendOptionsDialog(Mundane, $"It'll cost you {_cost} gold for this service.", opts2.ToArray());
                    }
                }
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new (0x03, "Let's do that."),
                        new (0x02, "Not now.")
                    };

                    client.SendOptionsDialog(Mundane, "You've heard of my service then? I'm able to increase the quality of an item for a fee.", options.ToArray());
                }
                break;
            case 0x02:
                {
                    client.SendOptionsDialog(Mundane, "You know where I'll be.");
                }
                break;
            case 0x03:
                {
                    var qualitySort = NpcShopExtensions.GetCharacterDetailingByteListForMidGradePolish(client);

                    if (qualitySort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Your backpack doesn't have any items that I can detail for you.");
                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "I can polish and upgrade Damaged or Common items. Pick an item you'd like polished.", qualitySort);
                    }
                }
                break;
            case 0x19:
                {
                    _itemDetail = NpcShopExtensions.ItemDetail;

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
                                        case Item.Quality.Uncommon:
                                        case Item.Quality.Rare:
                                        case Item.Quality.Epic:
                                        case Item.Quality.Legendary:
                                        case Item.Quality.Forsaken:
                                        case Item.Quality.Mythic:
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
                                        case Item.Quality.Rare:
                                        case Item.Quality.Epic:
                                        case Item.Quality.Legendary:
                                        case Item.Quality.Forsaken:
                                        case Item.Quality.Mythic:
                                            _itemDetail.ItemQuality = Item.Quality.Uncommon;
                                            break;
                                        default:
                                            client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                            break;
                                    }
                                    break;
                                case Item.Quality.Uncommon:
                                    switch (_itemDetail.OriginalQuality)
                                    {
                                        case Item.Quality.Damaged:
                                        case Item.Quality.Common:
                                            break;
                                        case Item.Quality.Uncommon:
                                            _itemDetail.OriginalQuality = Item.Quality.Rare;
                                            _itemDetail.ItemQuality = Item.Quality.Rare;
                                            break;
                                        case Item.Quality.Rare:
                                        case Item.Quality.Epic:
                                        case Item.Quality.Legendary:
                                        case Item.Quality.Forsaken:
                                        case Item.Quality.Mythic:
                                            _itemDetail.ItemQuality = Item.Quality.Rare;
                                            break;
                                        default:
                                            client.SendOptionsDialog(Mundane, "Sorry, I don't have the skill to do that.");
                                            break;
                                    }
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
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, good as new!");
                        client.SendOptionsDialog(Mundane, $"I put a lot of work in your {_itemDetail?.DisplayName}{{=a, hope you like it.");
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you don't have enough.");
                    }
                }

                break;
            case 0x20:
                {
                    client.PendingItemSessions = null;
                    client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (item.Template.CanStack || !item.Template.Enchantable)
        {
            client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, I can't polish that");
            return;
        }

        if (item.OriginalQuality is Item.Quality.Uncommon or Item.Quality.Common or Item.Quality.Damaged)
        {
            OnResponse(client, 0x00, item.InventorySlot.ToString());
            return;
        }

        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, sorry I don't have the skill to polish that");
    }
}