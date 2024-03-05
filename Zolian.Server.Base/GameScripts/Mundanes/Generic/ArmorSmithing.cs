using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("ArmorSmithing")]
public class ArmorSmithing(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private Item _itemDetail;
    private string _forgeNeededStoneOne;
    private string _forgeNeededStoneTwo;
    private string _forgeNeededStoneThree;
    private string _forgeNeededStoneFour;
    private uint _cost;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        CheckRank(client);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.QuestManager.ArmorSmithingTier)
        {
            case "Novice":
                options.Add(new(0x04, "Improve Armor"));
                break;
            case "Apprentice": // 110
                options.Add(new(0x05, "Improve Armor"));
                break;
            case "Journeyman": // 180
                options.Add(new(0x06, "Advance Armor"));
                break;
            case "Expert": // 250
                //options.Add(new(0x07, "Enhance Armor"));
                break;
            case "Artisan": // 400
                //options.Add(new(0x08, "Enchant Armor"));
                break;
        }

        client.SendOptionsDialog(Mundane, "It's a lot of work to get the right gear. How can I help?\n\n" +
                                          $"Armorsmithing: {client.Aisling.QuestManager.ArmorSmithing}", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                {
                    _cost = NpcShopExtensions.GetArmorSmithingCosts(client, args);
                    _itemDetail = NpcShopExtensions.ItemDetail;

                    if (_cost == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                        return;
                    }

                    if (_itemDetail == null)
                    {
                        client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                        return;
                    }

                    var opts2 = new List<Dialog.OptionsDataItem>
                    {
                        new(0x80, "And the catalysts?"),
                        new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage)
                    };

                    client.SendOptionsDialog(Mundane, $"It's going to cost about {_cost} gold to attempt this", opts2.ToArray());
                    break;
                }

            #region Obtaining Weapon to Reforge

            case 0x04:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterNoviceArmorImprove(client);

                    if (blacksmithingSort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Hmm, it seems you're out of things to reforge");

                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "Alright Novice, what do we want to work on?", blacksmithingSort);
                    }
                    break;
                }
            case 0x05:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterApprenticeArmorImprove(client);

                    if (blacksmithingSort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Hmm, it seems you're out of things to reforge");

                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "My Apprentice, fetch me some coals and we'll work on that item you wanted", blacksmithingSort);
                    }
                    break;
                }
            case 0x06:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterJourneymanArmorImprove(client);

                    if (blacksmithingSort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Hmm, it seems you're out of things to reforge");

                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "Listen up Journeyman, watch what I do, now you repeat it!", blacksmithingSort);
                    }
                    break;
                }
            case 0x07:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterExpertArmorImprove(client);

                    if (blacksmithingSort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Hmm, it seems you're out of things to reforge");

                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "Getting better everyday Expert, what is it we're working on today?", blacksmithingSort);
                    }
                    break;
                }
            case 0x08:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterArtisanalArmorImprove(client);

                    if (blacksmithingSort.Count == 0)
                    {
                        client.SendOptionsDialog(Mundane, "Hmm, it seems you're out of things to reforge");

                    }
                    else
                    {
                        client.SendItemSellDialog(Mundane, "Artisan, I'm always impressed by your works", blacksmithingSort);
                    }
                    break;
                }

            #endregion

            #region Catalyst Check

            case 0x80:
                {
                    var item = _itemDetail;
                    if (item == null)
                    {
                        client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                        return;
                    }

                    switch (item.ItemMaterial)
                    {
                        default:
                        case Item.ItemMaterials.None:
                            {
                                _forgeNeededStoneOne = "Refined Copper";
                                _forgeNeededStoneTwo = "Reict Weed";
                                _forgeNeededStoneThree = "Bee Wax";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Copper", 3))
                                {
                                    if (client.Aisling.HasInInventory("Reict Weed", 5))
                                    {
                                        if (client.Aisling.HasInInventory("Bee Wax", 6))
                                        {
                                            opts.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 6");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 5");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 3");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x3\n" +
                                                                  $"{_forgeNeededStoneTwo} x5\n" +
                                                                  $"{_forgeNeededStoneThree} x6", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Copper:
                            {
                                _forgeNeededStoneOne = "Refined Dark Iron";
                                _forgeNeededStoneTwo = "Reict Weed";
                                _forgeNeededStoneThree = "Raw Wax";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Dark Iron", 3))
                                {
                                    if (client.Aisling.HasInInventory("Reict Weed", 3))
                                    {
                                        if (client.Aisling.HasInInventory("Raw Wax", 6))
                                        {
                                            opts.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 6");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 3");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 3");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x3\n" +
                                                                  $"{_forgeNeededStoneTwo} x3\n" +
                                                                  $"{_forgeNeededStoneThree} x6", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Iron:
                            {
                                _forgeNeededStoneOne = "Refined Dark Iron";
                                _forgeNeededStoneTwo = "Refined Cobalt Steel";
                                _forgeNeededStoneThree = "Bocan Branch";
                                _forgeNeededStoneFour = "Scorpion Venom";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Dark Iron", 2))
                                {
                                    if (client.Aisling.HasInInventory("Refined Cobalt Steel", 2))
                                    {
                                        if (client.Aisling.HasInInventory("Bocan Branch", 3))
                                        {
                                            if (client.Aisling.HasInInventory("Scorpion Venom", 5))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 5");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 3");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 2");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 2");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x2\n" +
                                                                  $"{_forgeNeededStoneTwo} x2\n" +
                                                                  $"{_forgeNeededStoneThree} x3\n" +
                                                                  $"{_forgeNeededStoneFour} x5", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Steel:
                            {
                                _forgeNeededStoneOne = "Refined Dark Iron";
                                _forgeNeededStoneTwo = "Refined Cobalt Steel";
                                _forgeNeededStoneThree = "Scorpion Venom";
                                _forgeNeededStoneFour = "Spore Sac";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Dark Iron", 4))
                                {
                                    if (client.Aisling.HasInInventory("Refined Cobalt Steel", 4))
                                    {
                                        if (client.Aisling.HasInInventory("Scorpion Venom", 2))
                                        {
                                            if (client.Aisling.HasInInventory("Spore Sac", 3))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 3");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 2");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 4");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 4");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x4\n" +
                                                                  $"{_forgeNeededStoneTwo} x4\n" +
                                                                  $"{_forgeNeededStoneThree} x2\n" +
                                                                  $"{_forgeNeededStoneFour} x3", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Forged:
                            {
                                _forgeNeededStoneOne = "Refined Talos";
                                _forgeNeededStoneTwo = "Prahed Bellis";
                                _forgeNeededStoneThree = "Wisp Dust";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Talos", 10))
                                {
                                    if (client.Aisling.HasInInventory("Prahed Bellis", 6))
                                    {
                                        if (client.Aisling.HasInInventory("Wisp Dust", 25))
                                        {
                                            opts.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 25");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 6");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 10");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x10\n" +
                                                                  $"{_forgeNeededStoneTwo} x6\n" +
                                                                  $"{_forgeNeededStoneThree} x25", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Elven:
                            {
                                _forgeNeededStoneOne = "Refined Cobalt Steel";
                                _forgeNeededStoneTwo = "Refined Hybrasyl";
                                _forgeNeededStoneThree = "Wraith Blood";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Cobalt Steel", 6))
                                {
                                    if (client.Aisling.HasInInventory("Refined Hybrasyl", 3))
                                    {
                                        if (client.Aisling.HasInInventory("Wraith Blood", 9))
                                        {
                                            opts.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 9");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 3");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 6");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x6\n" +
                                                                  $"{_forgeNeededStoneTwo} x3\n" +
                                                                  $"{_forgeNeededStoneThree} x9", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Dwarven:
                            {
                                _forgeNeededStoneOne = "Refined Talos";
                                _forgeNeededStoneTwo = "Refined Copper";
                                _forgeNeededStoneThree = "Fairy Wing";
                                _forgeNeededStoneFour = "Nautilus Shell";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Talos", 6))
                                {
                                    if (client.Aisling.HasInInventory("Refined Copper", 5))
                                    {
                                        if (client.Aisling.HasInInventory("Fairy Wing", 25))
                                        {
                                            if (client.Aisling.HasInInventory("Nautilus Shell", 8))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 8");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 25");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 5");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 6");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x6\n" +
                                                                  $"{_forgeNeededStoneTwo} x5\n" +
                                                                  $"{_forgeNeededStoneThree} x25\n" +
                                                                  $"{_forgeNeededStoneFour} x8", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Mythril:
                            {
                                _forgeNeededStoneOne = "Refined Hybrasyl";
                                _forgeNeededStoneTwo = "Aiten Bloom";
                                _forgeNeededStoneThree = "Sea Twine";
                                _forgeNeededStoneFour = "Spiked Nautilus Shell";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Hybrasyl", 10))
                                {
                                    if (client.Aisling.HasInInventory("Aiten Bloom", 6))
                                    {
                                        if (client.Aisling.HasInInventory("Sea Twine", 8))
                                        {
                                            if (client.Aisling.HasInInventory("Spiked Nautilus Shell", 6))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 6");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 8");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 6");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 10");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x10\n" +
                                                                  $"{_forgeNeededStoneTwo} x6\n" +
                                                                  $"{_forgeNeededStoneThree} x8\n" +
                                                                  $"{_forgeNeededStoneFour} x6", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Hybrasyl:
                        case Item.ItemMaterials.MoonStone:
                        case Item.ItemMaterials.SunStone:
                        case Item.ItemMaterials.Ebony:
                        case Item.ItemMaterials.Runic:
                        case Item.ItemMaterials.Chaos:
                            client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                            return;
                    }

                    break;
                }

            #endregion

            // Reset Npc
            case 0x81:
                {
                    _forgeNeededStoneOne = "";
                    _forgeNeededStoneTwo = "";
                    _forgeNeededStoneThree = "";
                    _forgeNeededStoneFour = "";
                    _itemDetail = null;
                    _cost = 0;
                    TopMenu(client);
                    break;
                }
            // Take Items & Reforge
            case 0x82:
                {
                    var item = _itemDetail;
                    var chance = Generator.RandomNumPercentGen();

                    if (client.Aisling.Luck > 0)
                        for (var i = 0; i < client.Aisling.Luck; i++)
                        {
                            if (chance - 0.05 < 0)
                            {
                                chance = 0;
                                continue;
                            }

                            chance -= 0.05;
                        }

                    if (item == null)
                    {
                        client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                        return;
                    }

                    if (client.Aisling.GoldPoints >= _cost)
                    {
                        switch (item.ItemMaterial)
                        {
                            default:
                            case Item.ItemMaterials.None:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Copper");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Reict Weed");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Bee Wax");
                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 3);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 5);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 6);

                                    if (chance >= .90)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Novice")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Copper;
                                }
                                break;
                            case Item.ItemMaterials.Copper:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Reict Weed");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Raw Wax");
                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 3);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 3);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 6);

                                    if (chance >= .85)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Novice" or "Apprentice")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Iron;
                                }
                                break;
                            case Item.ItemMaterials.Iron:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Bocan Branch");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Scorpion Venom");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 2);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 2);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 3);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 5);

                                    if (chance >= .80)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Apprentice")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Steel;
                                }
                                break;
                            case Item.ItemMaterials.Steel:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Scorpion Venom");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Spore Sac");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 4);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 4);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 2);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 3);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Apprentice")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Forged;
                                }
                                break;
                            case Item.ItemMaterials.Forged:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Talos");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Prahed Bellis");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Wisp Dust");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 10);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 6);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 25);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Apprentice" or "Journeyman")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Elven;
                                }
                                break;
                            case Item.ItemMaterials.Elven:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Hybrasyl");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Wraith Blood");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 6);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 3);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 9);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Journeyman")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Dwarven;
                                }
                                break;
                            case Item.ItemMaterials.Dwarven:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Talos");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Copper");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Fairy Wing");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Nautilus Shell");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 6);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 5);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 25);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 8);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Journeyman")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Mythril;
                                }
                                break;
                            case Item.ItemMaterials.Mythril:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Hybrasyl");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Aiten Bloom");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Sea Twine");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Spiked Nautilus Shell");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 10);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 6);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 8);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 6);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Journeyman")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Hybrasyl;
                                }
                                break;
                            case Item.ItemMaterials.Hybrasyl:
                            case Item.ItemMaterials.MoonStone:
                            case Item.ItemMaterials.SunStone:
                            case Item.ItemMaterials.Ebony:
                            case Item.ItemMaterials.Runic:
                            case Item.ItemMaterials.Chaos:
                                client.SendOptionsDialog(Mundane, "That armor is quite beautiful, we can't do anything further with it.");
                                return;
                        }

                        client.Aisling.GoldPoints -= _cost;
                        client.Aisling.Inventory.UpdateSlot(client, _itemDetail);
                        client.SendAttributes(StatUpdateType.WeightGold);
                        client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Nicely done!");
                        client.SendOptionsDialog(Mundane, $"Nicely done {client.Aisling.Username}!");
                        CheckRank(client);
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "My coal isn't free, pay the fee to keep the fire going!");
                    }
                    break;
                }
        }
    }

    private void CheckRank(WorldClient client)
    {
        switch (client.Aisling.QuestManager.ArmorSmithing)
        {
            case >= 0 and <= 35:
                if (!client.Aisling.LegendBook.Has("Armorsmithing: Novice"))
                {
                    client.Aisling.QuestManager.ArmorSmithingTier = "Novice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmorS1",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Armorsmithing: Novice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 110:
                if (!client.Aisling.LegendBook.Has("Armorsmithing: Apprentice"))
                {
                    client.Aisling.QuestManager.ArmorSmithingTier = "Apprentice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmorS2",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Armorsmithing: Apprentice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 180:
                if (!client.Aisling.LegendBook.Has("Armorsmithing: Journeyman"))
                {
                    client.Aisling.QuestManager.ArmorSmithingTier = "Journeyman";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmorS3",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Armorsmithing: Journeyman"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 250:
                if (!client.Aisling.LegendBook.Has("Armorsmithing: Expert"))
                {
                    client.Aisling.QuestManager.ArmorSmithingTier = "Expert";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmorS4",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Armorsmithing: Expert"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 400:
                if (!client.Aisling.LegendBook.Has("Armorsmithing: Artisan"))
                {
                    client.Aisling.QuestManager.ArmorSmithingTier = "Artisan";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LArmorS5",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Armorsmithing: Artisan"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
        }
    }
}