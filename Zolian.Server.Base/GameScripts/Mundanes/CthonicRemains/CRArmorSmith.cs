using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("CRArmorSmith")]
public class CrArmorSmith(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.HonoringTheFallen &&
            client.Aisling.HasItem("Rouel's Tarnished Armor") &&
            client.Aisling.HasItem("Teardrop Ruby") &&
            client.Aisling.HasItem("Silver Ingot"))
            options.Add(new(0x300, "Reforging Rouel's Armor"));

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
            case "Expert" when client.Aisling.QuestManager.ArmorCraftingCodexLearned: // 250
            case "Artisan" when client.Aisling.QuestManager.ArmorCraftingCodexLearned:
                options.Add(new(0x07, "Enhance Armor"));

                // Questing to Artisan
                if (!client.Aisling.QuestManager.ArmorCraftingAdvancedCodexLearned)
                    options.Add(new(0x89, "Advanced Codex Research"));

                if (client.Aisling.HasItem("Bahamut's Blood Ruby Eye"))
                    options.Add(new(0x90, "I managed to obtain this"));

                break;
        }

        if (client.Aisling.QuestManager.ArmorCraftingAdvancedCodexLearned)
        {
            client.SendOptionsDialog(Mundane, $"Artisan {client.Aisling.Username}, you're always welcome to use my anvil.\n\nArmorsmithing: {client.Aisling.QuestManager.ArmorSmithing}", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.QuestManager.ArmorCraftingCodexLearned
                ? $"Hail! {client.Aisling.Username}, let's get started!\n\nArmorsmithing: {client.Aisling.QuestManager.ArmorSmithing}"
                : $"It's a lot of work to get the right gear. How can I help?\n\nArmorsmithing: {client.Aisling.QuestManager.ArmorSmithing}", options.ToArray());
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

            #region Obtaining Armor to Reforge

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

                    // Obtains the current item material and checks the required materials to reforge the item to the next tier.
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
                            {
                                _forgeNeededStoneOne = "Refined Cobalt Steel";
                                _forgeNeededStoneTwo = "Refined Obsidian";
                                _forgeNeededStoneThree = "Ancient Bones";
                                _forgeNeededStoneFour = "Fairy Wing";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Cobalt Steel", 5))
                                {
                                    if (client.Aisling.HasInInventory("Refined Obsidian", 4))
                                    {
                                        if (client.Aisling.HasInInventory("Ancient Bones", 10))
                                        {
                                            if (client.Aisling.HasInInventory("Fairy Wing", 7))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 7");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            client.SendOptionsDialog(Mundane,
                                                $"Seem to be missing some {_forgeNeededStoneThree}\n" +
                                                "Need: 10");
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
                                                                      "Need: 5");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x5\n" +
                                                                  $"{_forgeNeededStoneTwo} x4\n" +
                                                                  $"{_forgeNeededStoneThree} x10\n" +
                                                                  $"{_forgeNeededStoneFour} x7", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.MoonStone:
                            {
                                _forgeNeededStoneOne = "Refined Cobalt Steel";
                                _forgeNeededStoneTwo = "Refined Obsidian";
                                _forgeNeededStoneThree = "Flawless Ruby";
                                _forgeNeededStoneFour = "Captured Golden Floppy";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Cobalt Steel", 10))
                                {
                                    if (client.Aisling.HasInInventory("Refined Obsidian", 8))
                                    {
                                        if (client.Aisling.HasInInventory("Flawless Ruby", 2))
                                        {
                                            if (client.Aisling.HasInInventory("Captured Golden Floppy", 3))
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
                                            "Need: 8");
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
                                                                  $"{_forgeNeededStoneTwo} x8\n" +
                                                                  $"{_forgeNeededStoneThree} x2\n" +
                                                                  $"{_forgeNeededStoneFour} x3", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.SunStone:
                            {
                                _forgeNeededStoneOne = "Refined Obsidian";
                                _forgeNeededStoneTwo = "Refined Dark Iron";
                                _forgeNeededStoneThree = "Flawless Ruby";
                                _forgeNeededStoneFour = "Flawless Sapphire";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Obsidian", 15))
                                {
                                    if (client.Aisling.HasInInventory("Refined Dark Iron", 10))
                                    {
                                        if (client.Aisling.HasInInventory("Flawless Ruby", 2))
                                        {
                                            if (client.Aisling.HasInInventory("Flawless Sapphire", 2))
                                            {
                                                opts.Add(new(0x82, "Reforge"));
                                            }
                                            else
                                            {
                                                client.SendOptionsDialog(Mundane,
                                                    $"Seem to be missing some {_forgeNeededStoneFour}\n" +
                                                    "Need: 2");
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
                                            "Need: 10");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 15");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x15\n" +
                                                                  $"{_forgeNeededStoneTwo} x10\n" +
                                                                  $"{_forgeNeededStoneThree} x2\n" +
                                                                  $"{_forgeNeededStoneFour} x2", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Ebony: // up to here for expert
                            {
                                _forgeNeededStoneOne = "Refined Obsidian";
                                _forgeNeededStoneTwo = "Omega Module";
                                _forgeNeededStoneThree = "Flawless Ruby";
                                _forgeNeededStoneFour = "Flawless Sapphire";
                                var opts = new List<Dialog.OptionsDataItem>();

                                if (client.Aisling.HasInInventory("Refined Obsidian", 20))
                                {
                                    if (client.Aisling.HasInInventory("Omega Module", 1))
                                    {
                                        if (client.Aisling.HasInInventory("Flawless Ruby", 5))
                                        {
                                            if (client.Aisling.HasInInventory("Flawless Sapphire", 5))
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
                                                "Need: 5");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            $"Seem to be missing some {_forgeNeededStoneTwo}\n" +
                                            "Need: 1");
                                        return;
                                    }
                                }
                                else
                                {
                                    client.SendOptionsDialog(Mundane, $"Seem to be missing some {_forgeNeededStoneOne}\n" +
                                                                      "Need: 20");
                                    return;
                                }

                                opts.Add(new(0x81, ServerSetup.Instance.Config.MerchantCancelMessage));

                                client.SendOptionsDialog(Mundane, $"It will also take:\n" +
                                                                  $"{_forgeNeededStoneOne} x20\n" +
                                                                  $"{_forgeNeededStoneTwo} x1\n" +
                                                                  $"{_forgeNeededStoneThree} x5\n" +
                                                                  $"{_forgeNeededStoneFour} x5", opts.ToArray());
                            }
                            break;
                        case Item.ItemMaterials.Runic:
                        case Item.ItemMaterials.Chaos:
                            {
                                client.SendOptionsDialog(Mundane, "That armor is quite beautiful, we can't do anything further with it.");
                                return;
                            }
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
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Ancient Bones");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Fairy Wing");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 5);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 4);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 10);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 7);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Expert")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    if (!client.Aisling.QuestManager.CraftedMoonArmor)
                                        client.Aisling.QuestManager.CraftedMoonArmor = true;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.MoonStone;
                                }
                                break;
                            case Item.ItemMaterials.MoonStone:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Cobalt Steel");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Flawless Ruby");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Captured Golden Floppy");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 10);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 8);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 2);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 3);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Expert")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.SunStone;
                                }
                                break;
                            case Item.ItemMaterials.SunStone:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Refined Dark Iron");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Flawless Ruby");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Flawless Sapphire");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 15);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 10);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 2);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 2);

                                    if (chance >= .75)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Expert")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Ebony;
                                }
                                break;
                            case Item.ItemMaterials.Ebony:
                                {
                                    var noneReqOne = client.Aisling.HasItemReturnItem("Refined Obsidian");
                                    var noneReqTwo = client.Aisling.HasItemReturnItem("Omega Module");
                                    var noneReqThree = client.Aisling.HasItemReturnItem("Flawless Ruby");
                                    var noneReqFour = client.Aisling.HasItemReturnItem("Flawless Sapphire");

                                    client.Aisling.Inventory.RemoveRange(client, noneReqOne, 20);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqTwo, 1);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqThree, 5);
                                    client.Aisling.Inventory.RemoveRange(client, noneReqFour, 5);

                                    if (chance >= .70)
                                    {
                                        client.SendOptionsDialog(Mundane,
                                            "Ah, failed.. Don't worry, lets try again. I won't take your money..\n" +
                                            "But unfortunately the materials were used up.");
                                        return;
                                    }

                                    if (client.Aisling.QuestManager.ArmorSmithingTier is "Expert")
                                        client.Aisling.QuestManager.ArmorSmithing++;

                                    _itemDetail.ItemMaterial = Item.ItemMaterials.Runic;
                                }
                                break;
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

            #region Questing

            case 0x89:
                {
                    client.SendOptionsDialog(Mundane, $"Unfortunately, our forge isn't hot enough to go further into the craft. Looks like if you haven't already, you'll have to visit " +
                                                      $"my brother in {{=bChaos{{=a. He has molten rock hot enough. However, you'll need a ruby infused with dragon fire as a catalyst.");
                    break;
                }
            case 0x90:
                {
                    client.SendOptionsDialog(Mundane, "Once again, you are astounding! Look for my brother in the lower levels of {=bChaos {=aand he'll see about reforging your armor with this {=bBahamut's Blood Ruby Eye{=a.");
                    break;
                }
            case 0x300:
                {
                    var opts2 = new List<Dialog.OptionsDataItem>
                    {
                        new(0x301, "Can it be done?")
                    };

                    client.SendOptionsDialog(Mundane, $"Oh.. that lad who gave his life during The Great War?", opts2.ToArray());
                }
                break;
            case 0x301:
                {
                    var opts2 = new List<Dialog.OptionsDataItem>();

                    if (client.Aisling.HasItem("Rouel's Tarnished Armor") &&
                        client.Aisling.HasItem("Teardrop Ruby") &&
                        client.Aisling.HasItem("Silver Ingot"))
                    {
                        opts2.Add(new(0x302, "I have them right here."));
                    }
                    else
                        opts2.Add(new(0x303, "I'll be back"));

                    client.SendOptionsDialog(Mundane, $"Of course, I'll need a few items.\nRouel's Tarnished Armor, Teardrop Ruby, and a Silver Ingot", opts2.ToArray());
                }
                break;
            case 0x302:
                {
                    var item1 = client.Aisling.HasItemReturnItem("Rouel's Tarnished Armor");
                    var item2 = client.Aisling.HasItemReturnItem("Teardrop Ruby");
                    var item3 = client.Aisling.HasItemReturnItem("Silver Ingot");

                    if (item1 != null && item2 != null && item3 != null)
                    {
                        client.Aisling.QuestManager.LouresReputation++;
                        client.Aisling.Inventory.RemoveFromInventory(client, item1);
                        client.Aisling.Inventory.RemoveFromInventory(client, item2);
                        client.Aisling.Inventory.RemoveFromInventory(client, item3);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "A flash of light happens!");
                        var legend = new Legend.LegendItem
                        {
                            Key = "LRouelArm",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.RedPurpleG3,
                            Icon = (byte)LegendIcon.Warrior,
                            Text = "Reforged Rouel's Armor"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                        CraftRouelsArmor(client);
                        client.SendAttributes(StatUpdateType.WeightGold);
                    }
                    else
                        client.CloseDialog();
                }
                break;
            case 0x303:
                {
                    client.CloseDialog();
                }
                break;
                #endregion
        }
    }

    private void CraftRouelsArmor(WorldClient client)
    {
        var armor = new Item();

        switch (client.Aisling.Path)
        {
            case Class.Berserker:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Jupe" : "Rouel's Lagertha");
                break;
            case Class.Defender:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Donet" : "Rouel's Bliaut");
                break;
            case Class.Assassin:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Kit" : "Rouel's Kagume");
                break;
            case Class.Cleric:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Vestments" : "Rouel's Stoller");
                break;
            case Class.Arcanus:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Robes" : "Rouel's Cloak");
                break;
            case Class.Monk:
                armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Rouel's Gi" : "Rouel's Lotus");
                break;
            case Class.Peasant:
            case Class.DualBash:
            case Class.DualCast:
            case Class.Racial:
            case Class.Monster:
            case Class.Quest:
                break;
        }

        armor.GiveTo(client.Aisling);
        client.SendOptionsDialog(Mundane, $"Well now.. that came out better than I expected. Try it on!");
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
                        IsPublic = true,
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
                        IsPublic = true,
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
                        IsPublic = true,
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
                        IsPublic = true,
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
                        IsPublic = true,
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