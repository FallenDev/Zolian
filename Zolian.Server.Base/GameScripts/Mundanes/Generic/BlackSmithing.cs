using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("BlackSmithing")]
public class BlackSmithing(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private string _tempSkillName;
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

        switch (client.Aisling.QuestManager.BlackSmithingTier)
        {
            case "Novice": // 25
                options.Add(new(0x04, "Improve Weapons"));
                break;
            case "Apprentice": // 75
                if (client.Aisling.HasItem("Basic Combo Scroll"))
                    options.Add(new(0x10, "Basic Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x05, "Improve Weapons"));
                break;
            case "Journeyman": // 150
                if (client.Aisling.HasItem("Basic Combo Scroll"))
                    options.Add(new(0x50, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x20, "Advanced Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x06, "Advance Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
            case "Expert": // 225
                if (client.Aisling.HasItem("Basic Combo Scroll") || client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x50, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Enhanced Combo Scroll"))
                    options.Add(new(0x30, "Enhanced Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x07, "Enhance Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
            case "Artisan":
                if (client.Aisling.HasItem("Basic Combo Scroll") || client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x50, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Enchanted Combo Scroll"))
                    options.Add(new(0x40, "Enchanted Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x08, "Enchant Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
        }

        client.SendOptionsDialog(Mundane, "*clank!* *clank!* Oh! Hey there, let's get to work.\n\n" +
                                          $"Blacksmithing: {client.Aisling.QuestManager.BlackSmithing}", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                {
                    _cost = NpcShopExtensions.GetSmithingCosts(client, args);
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
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x02, "I have the parchment")
                    };

                    client.SendOptionsDialog(Mundane, "We'll first need some parchment, I believe you can buy some in Rionnag", options.ToArray());
                    break;
                }
            case 0x02:
                {
                    if (client.Aisling.HasItem("Plain Parchment"))
                    {
                        var parchment = client.Aisling.HasItemReturnItem("Plain Parchment");
                        client.Aisling.Inventory.RemoveFromInventory(client, parchment);
                        var item = new Item();
                        item = item.Create(client.Aisling, "Basic Combo Scroll");
                        item.GiveTo(client.Aisling);
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.SendOptionsDialog(Mundane, "Very good, now let's write something on your scroll");
                    }
                    else
                        client.SendOptionsDialog(Mundane, "Are you sure, I don't quite see any on you?");

                    break;
                }
            case 0x03:
                {
                    client.SendServerMessage(ServerMessageType.ScrollWindow, "");
                    client.SendOptionsDialog(Mundane, "Here, this will help; *hands you a piece of parchment*");

                    break;
                }

            #region Obtaining Weapon to Reforge

            case 0x04:
                {
                    var blacksmithingSort = NpcShopExtensions.GetCharacterNoviceWeaponImprove(client);

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
                    var blacksmithingSort = NpcShopExtensions.GetCharacterApprenticeWeaponImprove(client);

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
                    var blacksmithingSort = NpcShopExtensions.GetCharacterJourneymanWeaponImprove(client);

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
                    var blacksmithingSort = NpcShopExtensions.GetCharacterExpertWeaponImprove(client);

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
                    var blacksmithingSort = NpcShopExtensions.GetCharacterArtisanWeaponImprove(client);

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

            // Upgrading Combo Scroll
            case 0x50:
                {
                    if (client.Aisling.HasItem("Basic Combo Scroll"))
                    {
                        var scroll = client.Aisling.HasItemReturnItem("Basic Combo Scroll");
                        client.Aisling.Inventory.RemoveFromInventory(client, scroll);
                        var item = new Item();
                        item = item.Create(client.Aisling, "Advanced Combo Scroll");
                        item.GiveTo(client.Aisling);
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.SendOptionsDialog(Mundane, "Nicely done!");
                        break;
                    }

                    if (client.Aisling.HasItem("Advanced Combo Scroll"))
                    {
                        var scroll = client.Aisling.HasItemReturnItem("Advanced Combo Scroll");
                        client.Aisling.Inventory.RemoveFromInventory(client, scroll);
                        var item = new Item();
                        item = item.Create(client.Aisling, "Enhanced Combo Scroll");
                        item.GiveTo(client.Aisling);
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.SendOptionsDialog(Mundane, "Well met!");
                        break;
                    }

                    if (client.Aisling.HasItem("Enhanced Combo Scroll"))
                    {
                        var scroll = client.Aisling.HasItemReturnItem("Enhanced Combo Scroll");
                        client.Aisling.Inventory.RemoveFromInventory(client, scroll);
                        var item = new Item();
                        item = item.Create(client.Aisling, "Enchanted Combo Scroll");
                        item.GiveTo(client.Aisling);
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.Aisling.QuestManager.BlackSmithing++;
                        client.SendOptionsDialog(Mundane, "Nicely done!");
                    }
                    else
                        client.SendOptionsDialog(Mundane, "Are you sure, I don't quite see any on you?");

                    break;
                }

            #region Catalyst Check

            case 0x80:
                {
                    var item = _itemDetail;
                    if (item == null)
                    {
                        client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                        return;
                    }

                    switch (item.GearEnhancement)
                    {
                        default:
                        case Item.GearEnhancements.None:
                            _forgeNeededStoneOne = "Refined Talos"; // 1
                            _forgeNeededStoneTwo = "Refined Copper"; // 1
                            var opts = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Talos", 1))
                            {
                                if (client.Aisling.HasInInventory("Refined Copper", 1))
                                {
                                    opts.Add(new(0x82, "Reforge"));
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Copper\n" +
                                                                      "Need: 1");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Talos\n" +
                                                                  "Need: 1");
                                return;
                            }

                            opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x1\n" +
                                                              $"{_forgeNeededStoneTwo} x1", opts.ToArray());
                            break;
                        case Item.GearEnhancements.One:
                            _forgeNeededStoneOne = "Refined Copper"; // 2
                            _forgeNeededStoneTwo = "Refined Hybrasyl"; // 1
                            _forgeNeededStoneThree = "Refined Dark Iron"; // 1
                            var opts1 = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Copper", 2))
                            {
                                if (client.Aisling.HasInInventory("Refined Hybrasyl", 1))
                                {
                                    if (client.Aisling.HasInInventory("Refined Dark Iron", 1))
                                    {
                                        opts1.Add(new(0x82, "Reforge"));
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Dark Iron\n" +
                                                                          "Need: 1");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Hybrasyl\n" +
                                                                      "Need: 1");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Copper\n" +
                                                                  "Need: 2");
                                return;
                            }

                            opts1.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x2\n" +
                                                              $"{_forgeNeededStoneTwo} x1\n" +
                                                              $"{_forgeNeededStoneThree} x1", opts1.ToArray());
                            break;
                        case Item.GearEnhancements.Two:
                            _forgeNeededStoneOne = "Refined Hybrasyl"; // 3
                            _forgeNeededStoneTwo = "Refined Dark Iron"; // 2
                            _forgeNeededStoneThree = "Refined Cobalt Steel"; // 1
                            var opts2 = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Hybrasyl", 3))
                            {
                                if (client.Aisling.HasInInventory("Refined Dark Iron", 2))
                                {
                                    if (client.Aisling.HasInInventory("Refined Cobalt Steel", 1))
                                    {
                                        opts2.Add(new(0x82, "Reforge"));
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Cobalt Steel\n" +
                                                                          "Need: 1");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Dark Iron\n" +
                                                                      "Need: 2");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Hybrasyl\n" +
                                                                  "Need: 3");
                                return;
                            }

                            opts2.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x3\n" +
                                                              $"{_forgeNeededStoneTwo} x2\n" +
                                                              $"{_forgeNeededStoneThree} x1", opts2.ToArray());
                            break;
                        case Item.GearEnhancements.Three:
                            _forgeNeededStoneOne = "Refined Hybrasyl"; // 3
                            _forgeNeededStoneTwo = "Refined Dark Iron"; // 3
                            _forgeNeededStoneThree = "Refined Cobalt Steel"; // 2
                            _forgeNeededStoneFour = "Refined Obsidian"; // 1
                            var opts3 = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Hybrasyl", 3))
                            {
                                if (client.Aisling.HasInInventory("Refined Dark Iron", 3))
                                {
                                    if (client.Aisling.HasInInventory("Refined Cobalt Steel", 2))
                                    {
                                        if (client.Aisling.HasInInventory("Refined Obsidian", 1))
                                        {
                                            opts3.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Obsidian\n" +
                                                                              "Need: 1");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Cobalt Steel\n" +
                                                                          "Need: 2");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Dark Iron\n" +
                                                                      "Need: 3");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Hybrasyl\n" +
                                                                  "Need: 3");
                                return;
                            }

                            opts3.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x3\n" +
                                                              $"{_forgeNeededStoneTwo} x3\n" +
                                                              $"{_forgeNeededStoneThree} x2\n" +
                                                              $"{_forgeNeededStoneFour} x1", opts3.ToArray());
                            break;
                        case Item.GearEnhancements.Four:
                            _forgeNeededStoneOne = "Refined Dark Iron"; // 3
                            _forgeNeededStoneTwo = "Refined Cobalt Steel"; // 3
                            _forgeNeededStoneThree = "Refined Obsidian"; // 1
                            _forgeNeededStoneFour = "Flawless Ruby"; // 1
                            var opts4 = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Dark Iron", 3))
                            {
                                if (client.Aisling.HasInInventory("Refined Cobalt Steel", 3))
                                {
                                    if (client.Aisling.HasInInventory("Refined Obsidian", 1))
                                    {
                                        if (client.Aisling.HasInInventory("Flawless Ruby", 1))
                                        {
                                            opts4.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane, "Seem to be missing a Flawless Ruby\n" +
                                                                              "Need: 1");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Obsidian\n" +
                                                                          "Need: 1");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Cobalt Steel\n" +
                                                                      "Need: 3");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Dark Iron\n" +
                                                                  "Need: 3");
                                return;
                            }

                            opts4.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x3\n" +
                                                              $"{_forgeNeededStoneTwo} x3\n" +
                                                              $"{_forgeNeededStoneThree} x1\n" +
                                                              $"{_forgeNeededStoneFour} x1", opts4.ToArray());
                            break;
                        case Item.GearEnhancements.Five:
                            _forgeNeededStoneOne = "Refined Cobalt Steel"; // 3
                            _forgeNeededStoneTwo = "Refined Obsidian"; // 2
                            _forgeNeededStoneThree = "Flawless Ruby"; // 1
                            _forgeNeededStoneFour = "Flawless Sapphire"; // 1
                            var opts5 = new List<Dialog.OptionsDataItem>();

                            if (client.Aisling.HasInInventory("Refined Cobalt Steel", 3))
                            {
                                if (client.Aisling.HasInInventory("Refined Obsidian", 2))
                                {
                                    if (client.Aisling.HasInInventory("Flawless Ruby", 1))
                                    {
                                        if (client.Aisling.HasInInventory("Flawless Sapphire", 1))
                                        {
                                            opts5.Add(new(0x82, "Reforge"));
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane, "Seem to be missing a Flawless Sapphire\n" +
                                                                              "Need: 1");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane, "Seem to be missing a Flawless Ruby\n" +
                                                                          "Need: 1");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Obsidian\n" +
                                                                      "Need: 2");
                                    return;
                                }
                            }
                            else
                            {
                                client.SendOptionsDialog(Mundane, "Seem to be missing some Refined Cobalt Steel\n" +
                                                                  "Need: 3");
                                return;
                            }

                            opts5.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                            client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                              $"{_forgeNeededStoneOne} x3\n" +
                                                              $"{_forgeNeededStoneTwo} x2\n" +
                                                              $"{_forgeNeededStoneThree} x1\n" +
                                                              $"{_forgeNeededStoneFour} x1", opts5.ToArray());
                            break;
                        case Item.GearEnhancements.Six:
                            client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
                            return;
                    }

                    break;
                }

            #endregion

            // Reset Npc
            case 0x81:
                {
                    _tempSkillName = "";
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
                        switch (item.GearEnhancement)
                        {
                            default:
                            case Item.GearEnhancements.None:
                                var noneReqOne = client.Aisling.HasItemReturnItem("Refined Talos");
                                var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Copper");
                                client.Aisling.Inventory.RemoveRange(client, noneReqOne, 1);
                                client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 1);

                                if (chance >= .70)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Novice" or "Apprentice")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.One;
                                break;
                            case Item.GearEnhancements.One:
                                var oneReqOne = client.Aisling.HasItemReturnItem("Refined Copper");
                                var oneReqTwo = client.Aisling.HasItemReturnItem("Refined Hybrasyl");
                                var oneReqThree = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                client.Aisling.Inventory.RemoveRange(client, oneReqOne, 2);
                                client.Aisling.Inventory.RemoveRange(client, oneReqTwo, 1);
                                client.Aisling.Inventory.RemoveRange(client, oneReqThree, 1);

                                if (chance >= .50)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Apprentice" or "Journeyman")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.Two;
                                break;
                            case Item.GearEnhancements.Two:
                                var twoReqOne = client.Aisling.HasItemReturnItem("Refined Hybrasyl");
                                var twoReqTwo = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                var twoReqThree = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                client.Aisling.Inventory.RemoveRange(client, twoReqOne, 3);
                                client.Aisling.Inventory.RemoveRange(client, twoReqTwo, 2);
                                client.Aisling.Inventory.RemoveRange(client, twoReqThree, 1);

                                if (chance >= .30)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Journeyman" or "Expert")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.Three;
                                break;
                            case Item.GearEnhancements.Three:
                                var threeReqOne = client.Aisling.HasItemReturnItem("Refined Hybrasyl");
                                var threeReqTwo = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                var threeReqThree = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                var threeReqFour = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                client.Aisling.Inventory.RemoveRange(client, threeReqOne, 3);
                                client.Aisling.Inventory.RemoveRange(client, threeReqTwo, 3);
                                client.Aisling.Inventory.RemoveRange(client, threeReqThree, 2);
                                client.Aisling.Inventory.RemoveRange(client, threeReqFour, 1);

                                if (chance >= .20)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Journeyman" or "Expert" or "Artisan")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.Four;
                                break;
                            case Item.GearEnhancements.Four:
                                var fourReqOne = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                var fourReqTwo = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                var fourReqThree = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                var fourReqFour = client.Aisling.HasItemReturnItem("Flawless Ruby");
                                client.Aisling.Inventory.RemoveRange(client, fourReqOne, 3);
                                client.Aisling.Inventory.RemoveRange(client, fourReqTwo, 3);
                                client.Aisling.Inventory.RemoveRange(client, fourReqThree, 1);
                                client.Aisling.Inventory.RemoveRange(client, fourReqFour, 1);

                                if (chance >= .10)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Journeyman" or "Expert" or "Artisan")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.Five;
                                break;
                            case Item.GearEnhancements.Five:
                                var fiveReqOne = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                var fiveReqTwo = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                var fiveReqThree = client.Aisling.HasItemReturnItem("Flawless Ruby");
                                var fiveReqFour = client.Aisling.HasItemReturnItem("Flawless Sapphire");
                                client.Aisling.Inventory.RemoveRange(client, fiveReqOne, 3);
                                client.Aisling.Inventory.RemoveRange(client, fiveReqTwo, 2);
                                client.Aisling.Inventory.RemoveRange(client, fiveReqThree, 1);
                                client.Aisling.Inventory.RemoveRange(client, fiveReqFour, 1);

                                if (chance >= .10)
                                {
                                    client.SendOptionsDialog(Mundane, "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                                                      "But unfortunately the materials were used up.");
                                    return;
                                }

                                if (client.Aisling.QuestManager.BlackSmithingTier is "Journeyman" or "Expert" or "Artisan")
                                    client.Aisling.QuestManager.BlackSmithing++;

                                _itemDetail.GearEnhancement = Item.GearEnhancements.Six;
                                break;
                            case Item.GearEnhancements.Six:
                                client.SendOptionsDialog(Mundane, "Huh, there seems to be an issue. Let's try that again");
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

            #region Basic

            case 0x10:
                {
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Which order do you wish to set it? \nThese are the skills you currently possess.", 255, skillTemplateList);
                    break;
                }
            case 0x100:
                {
                    var foundTemplate = ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(args, out var skillTemplate);

                    if (foundTemplate)
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x101, $"{client.Aisling.ComboManager.Combo1 ?? "Skill 1"}"),
                            new(0x102, $"{client.Aisling.ComboManager.Combo2 ?? "Skill 2"}"),
                            new(0x103, $"{client.Aisling.ComboManager.Combo3 ?? "Skill 3"}")
                        };
                        _tempSkillName = skillTemplate.Name;

                        client.SendOptionsDialog(Mundane, "The order they're set in, is the order they'll execute.", options.ToArray());
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Hmm, let's try that again.");
                    break;
                }
            case 0x101:
                {
                    client.Aisling.ComboManager.Combo1 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 255, skillTemplateList);
                    break;
                }
            case 0x102:
                {
                    client.Aisling.ComboManager.Combo2 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 255, skillTemplateList);
                    break;
                }
            case 0x103:
                {
                    client.Aisling.ComboManager.Combo3 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 255, skillTemplateList);
                    break;
                }

            #endregion

            #region Advanced

            case 0x20:
                {
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Which order do you wish to set it? \nThese are the skills you currently possess.", 511, skillTemplateList);
                    break;
                }
            case 0x200:
                {
                    var foundTemplate = ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(args, out var skillTemplate);

                    if (foundTemplate)
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x201, $"{client.Aisling.ComboManager.Combo1 ?? "Skill 1"}"),
                            new(0x202, $"{client.Aisling.ComboManager.Combo2 ?? "Skill 2"}"),
                            new(0x203, $"{client.Aisling.ComboManager.Combo3 ?? "Skill 3"}"),
                            new(0x204, $"{client.Aisling.ComboManager.Combo4 ?? "Skill 4"}"),
                            new(0x205, $"{client.Aisling.ComboManager.Combo5 ?? "Skill 5"}"),

                        };
                        _tempSkillName = skillTemplate.Name;

                        client.SendOptionsDialog(Mundane, "The order they're set in, is the order they'll execute.", options.ToArray());
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Hmm, let's try that again.");
                    break;
                }
            case 0x201:
                {
                    client.Aisling.ComboManager.Combo1 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 511, skillTemplateList);
                    break;
                }
            case 0x202:
                {
                    client.Aisling.ComboManager.Combo2 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 511, skillTemplateList);
                    break;
                }
            case 0x203:
                {
                    client.Aisling.ComboManager.Combo3 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 511, skillTemplateList);
                    break;
                }
            case 0x204:
                {
                    client.Aisling.ComboManager.Combo4 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 511, skillTemplateList);
                    break;
                }
            case 0x205:
                {
                    client.Aisling.ComboManager.Combo5 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 511, skillTemplateList);
                    break;
                }

            #endregion

            #region Enhanced

            case 0x30:
                {
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Which order do you wish to set it? \nThese are the skills you currently possess.", 767, skillTemplateList);
                    break;
                }
            case 0x300:
                {
                    var foundTemplate = ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(args, out var skillTemplate);

                    if (foundTemplate)
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x301, $"{client.Aisling.ComboManager.Combo1 ?? "Skill 1"}"),
                            new(0x302, $"{client.Aisling.ComboManager.Combo2 ?? "Skill 2"}"),
                            new(0x303, $"{client.Aisling.ComboManager.Combo3 ?? "Skill 3"}"),
                            new(0x304, $"{client.Aisling.ComboManager.Combo4 ?? "Skill 4"}"),
                            new(0x305, $"{client.Aisling.ComboManager.Combo5 ?? "Skill 5"}"),
                            new(0x306, $"{client.Aisling.ComboManager.Combo6 ?? "Skill 6"}"),
                            new(0x307, $"{client.Aisling.ComboManager.Combo7 ?? "Skill 7"}")
                        };
                        _tempSkillName = skillTemplate.Name;

                        client.SendOptionsDialog(Mundane, "The order they're set in, is the order they'll execute.", options.ToArray());
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Hmm, let's try that again.");
                    break;
                }
            case 0x301:
                {
                    client.Aisling.ComboManager.Combo1 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x302:
                {
                    client.Aisling.ComboManager.Combo2 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x303:
                {
                    client.Aisling.ComboManager.Combo3 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x304:
                {
                    client.Aisling.ComboManager.Combo4 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x305:
                {
                    client.Aisling.ComboManager.Combo5 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x306:
                {
                    client.Aisling.ComboManager.Combo6 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }
            case 0x307:
                {
                    client.Aisling.ComboManager.Combo7 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 767, skillTemplateList);
                    break;
                }

            #endregion

            #region Enchanted

            case 0x40:
                {
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Which order do you wish to set it? \nThese are the skills you currently possess.", 1023, skillTemplateList);
                    break;
                }
            case 0x400:
                {
                    var foundTemplate = ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(args, out var skillTemplate);

                    if (foundTemplate)
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x401, $"{client.Aisling.ComboManager.Combo1 ?? "Skill 1"}"),
                            new(0x402, $"{client.Aisling.ComboManager.Combo2 ?? "Skill 2"}"),
                            new(0x403, $"{client.Aisling.ComboManager.Combo3 ?? "Skill 3"}"),
                            new(0x404, $"{client.Aisling.ComboManager.Combo4 ?? "Skill 4"}"),
                            new(0x405, $"{client.Aisling.ComboManager.Combo5 ?? "Skill 5"}"),
                            new(0x406, $"{client.Aisling.ComboManager.Combo6 ?? "Skill 6"}"),
                            new(0x407, $"{client.Aisling.ComboManager.Combo7 ?? "Skill 7"}"),
                            new(0x408, $"{client.Aisling.ComboManager.Combo8 ?? "Skill 8"}"),
                            new(0x409, $"{client.Aisling.ComboManager.Combo9 ?? "Skill 9"}"),
                            new(0x40A, $"{client.Aisling.ComboManager.Combo10 ?? "Skill 10"}")
                        };
                        _tempSkillName = skillTemplate.Name;

                        client.SendOptionsDialog(Mundane, "The order they're set in, is the order they'll execute.", options.ToArray());
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Hmm, let's try that again.");
                    break;
                }
            case 0x401:
                {
                    client.Aisling.ComboManager.Combo1 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x402:
                {
                    client.Aisling.ComboManager.Combo2 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x403:
                {
                    client.Aisling.ComboManager.Combo3 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x404:
                {
                    client.Aisling.ComboManager.Combo4 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x405:
                {
                    client.Aisling.ComboManager.Combo5 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x406:
                {
                    client.Aisling.ComboManager.Combo6 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x407:
                {
                    client.Aisling.ComboManager.Combo7 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x408:
                {
                    client.Aisling.ComboManager.Combo8 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x409:
                {
                    client.Aisling.ComboManager.Combo9 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }
            case 0x40A:
                {
                    client.Aisling.ComboManager.Combo10 = _tempSkillName;
                    var skillTemplateList = (from skill in client.Aisling.SkillBook.Skills.Values where skill != null select skill.Template).ToList();
                    client.SendSkillLearnDialog(Mundane, "Would you like to set another, or change one?", 1023, skillTemplateList);
                    break;
                }

                #endregion
        }
    }

    private void CheckRank(WorldClient client)
    {
        switch (client.Aisling.QuestManager.BlackSmithing)
        {
            case >= 0 and <= 24:
                if (!client.Aisling.LegendBook.Has("Blacksmithing: Novice"))
                {
                    client.Aisling.QuestManager.BlackSmithingTier = "Novice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LBlackS1",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Blacksmithing: Novice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 74:
                if (!client.Aisling.LegendBook.Has("Blacksmithing: Apprentice"))
                {
                    client.Aisling.QuestManager.BlackSmithingTier = "Apprentice";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LBlackS2",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Blacksmithing: Apprentice"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 149:
                if (!client.Aisling.LegendBook.Has("Blacksmithing: Journeyman"))
                {
                    client.Aisling.QuestManager.BlackSmithingTier = "Journeyman";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LBlackS3",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Blacksmithing: Journeyman"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 224:
                if (!client.Aisling.LegendBook.Has("Blacksmithing: Expert"))
                {
                    client.Aisling.QuestManager.BlackSmithingTier = "Expert";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LBlackS4",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Blacksmithing: Expert"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case <= 299:
                if (!client.Aisling.LegendBook.Has("Blacksmithing: Artisan"))
                {
                    client.Aisling.QuestManager.BlackSmithingTier = "Artisan";

                    var legend = new Legend.LegendItem
                    {
                        Key = "LBlackS5",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.BlueG1,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Blacksmithing: Artisan"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
        }
    }
}