using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Barber")]
public class Barber(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private int _styleNumber;
    private int _colorNumber;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>
        {
            new(0x02, "Style"),
            new(0x03, "Color"),
            new(0x04, "Buy Dye & Specialty Cuts")
        };

        client.SendOptionsDialog(Mundane, "Looking for a change? Ok, get in the chair.", opts.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        while (true)
        {
            switch (responseId)
            {
                case 0x01:
                    {
                        if (args != null && client.Aisling.Styling == 2)
                        {
                            int.TryParse(args, out var numbers);
                            if (numbers > 0)
                            {
                                _styleNumber = numbers;
                                responseId = 0x0A;
                                args = null;
                                continue;
                            }

                            responseId = 0x0D;
                            args = null;
                            continue;
                        }

                        if (args != null && client.Aisling.Coloring == 2)
                        {
                            int.TryParse(args, out var numbers);
                            if (numbers > 0)
                            {
                                _colorNumber = numbers;
                                responseId = 0x014;
                                args = null;
                                continue;
                            }

                            responseId = 0x0060;
                            args = null;
                            continue;
                        }
                    }
                    break;

                #region Styling

                case 0x02:
                    {
                        client.Aisling.Styling = 2;
                        client.Aisling.HairStyle = client.Aisling.OldStyle;
                        client.UpdateDisplay();
                        client.SendTextInput(Mundane,
                            "Which style are you interested in?\nMale Unavailable: 19\nFemale Unavailable: 18, 19", "1-61");
                    }
                    break;
                case 0x0A:
                    {
                        if (_styleNumber is 0 or 19 or > 61 ||
                            (client.Aisling.Gender == Gender.Female && _styleNumber is 18))
                        {
                            client.SendTextInput(Mundane,
                                "Please select a valid hairstyle:\nMale Unavailable: 19\nFemale Unavailable: 18, 19",
                                "1-61");
                        }
                        else
                        {
                            responseId = 0x0B;
                            args = null;
                            continue;
                        }
                    }
                    break;
                case 0x0B:
                    {
                        var opts = new List<Dialog.OptionsDataItem>
                    {
                        new(0x02, "It's not doing it for me"),
                        new(0x0C, "Let's go with that")
                    };

                        client.Aisling.Styling = 1;
                        client.Aisling.HairStyle = Convert.ToByte(_styleNumber);
                        client.UpdateDisplay();
                        client.SendOptionsDialog(Mundane,
                            $"So you're interested in #{_styleNumber}?\nIt'll cost you 5000 gold.", opts.ToArray());
                    }
                    break;
                case 0x0C:
                    {
                        if (client.Aisling.GoldPoints >= 5000)
                        {
                            client.Aisling.GoldPoints -= 5000;
                            client.SendServerMessage(ServerMessageType.ActiveMessage,
                                "Great! Thank you for the business, come again.");
                            client.Aisling.Styling = 0;
                            client.Aisling.OldStyle = client.Aisling.HairStyle;
                            client.SendAttributes(StatUpdateType.ExpGold);
                            TopMenu(client);
                        }
                        else
                        {
                            client.Aisling.HairStyle = client.Aisling.OldStyle;
                            client.UpdateDisplay();
                            client.SendOptionsDialog(Mundane, "No money, no style.");
                        }
                    }
                    break;
                case 0x0D:
                    {
                        client.SendTextInput(Mundane,
                            "Please select a valid hairstyle:\nMale Unavailable: 19\nFemale Unavailable: 18, 19", "1-61");
                    }
                    break;

                #endregion

                #region Coloring

                case 0x03:
                    {
                        client.Aisling.Coloring = 2;
                        client.Aisling.HairColor = client.Aisling.OldColor;
                        client.UpdateDisplay();
                        ColorList(client);
                        client.SendTextInput(Mundane, "Which dye would you like in your hair?", "0-55");
                    }
                    break;
                case 0x14:
                    {
                        if (_colorNumber is < 0 or > 55)
                        {
                            ColorList(client);
                            client.SendTextInput(Mundane, "Please select a valid color:", "0-55");
                        }
                        else
                        {
                            responseId = 0x15;
                            args = null;
                            continue;
                        }
                    }
                    break;
                case 0x15:
                    {
                        var opts = new List<Dialog.OptionsDataItem>
                    {
                        new(0x03, "It's not doing it for me"),
                        new(0x16, "Let's go with that")
                    };

                        client.Aisling.Coloring = 1;
                        client.Aisling.HairColor = Convert.ToByte(_colorNumber);
                        client.UpdateDisplay();
                        client.SendOptionsDialog(Mundane,
                            $"You're interested in #{_colorNumber}?\nIt'll cost you 20000 gold.", opts.ToArray());
                    }
                    break;
                case 0x16:
                    {
                        if (client.Aisling.GoldPoints >= 20000)
                        {
                            client.Aisling.GoldPoints -= 20000;
                            client.SendServerMessage(ServerMessageType.ActiveMessage,
                                "Great! Thank you for the business, come again.");
                            client.Aisling.Coloring = 0;
                            client.Aisling.OldColor = client.Aisling.HairColor;
                            client.SendAttributes(StatUpdateType.ExpGold);
                            TopMenu(client);
                        }
                        else
                        {
                            client.Aisling.HairColor = client.Aisling.OldColor;
                            client.UpdateDisplay();
                            client.SendOptionsDialog(Mundane, "No money, no color.");
                        }
                    }
                    break;
                case 0x17:
                    {
                        client.SendTextInput(Mundane, "Please select a valid color:", "0-55");
                    }
                    break;

                #endregion

                case 0x04:
                    client.SendItemShopDialog(Mundane, "These are the dyes I have.", 0x05,
                        NpcShopExtensions.BuyFromStoreInventory(Mundane));
                    break;
                case 0x05:
                    {
                        if (string.IsNullOrEmpty(args)) return;
                        var itemOrSlot = ushort.TryParse(args, out _);

                        if (!itemOrSlot)
                            NpcShopExtensions.BuyItemFromInventory(client, Mundane, args);
                    }
                    break;
                case 0x19:
                    {
                        if (client.PendingBuySessions != null)
                        {
                            var quantity = client.PendingBuySessions.Quantity;
                            var item = client.PendingBuySessions.Name;
                            var cost = (uint)(client.PendingBuySessions.Offer * client.PendingBuySessions.Quantity);
                            if (client.Aisling.GoldPoints >= cost)
                            {
                                client.Aisling.GoldPoints -= cost;
                                if (client.PendingBuySessions.Quantity > 1)
                                    client.GiveQuantity(client.Aisling, item, quantity);
                                else
                                {
                                    var itemCreated = new Item();
                                    var template = ServerSetup.Instance.GlobalItemTemplateCache[item];
                                    itemCreated = itemCreated.Create(client.Aisling, template);
                                    var given = itemCreated.GiveTo(client.Aisling);
                                    if (!given)
                                    {
                                        client.Aisling.BankManager.Items.TryAdd(itemCreated.ItemId, itemCreated);
                                        client.SendServerMessage(ServerMessageType.ActiveMessage,
                                            "Issue with giving you the item directly, deposited to bank");
                                    }
                                }

                                client.SendAttributes(StatUpdateType.Primary);
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.PendingBuySessions = null;
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cMuch appreciated!");
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
                            var item = client.Aisling.Inventory
                                .Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).FirstOrDefault();

                            if (item == null) return;

                            var offer = item.Template.Value / 2;

                            if (offer <= 0) return;
                            if (offer > item.Template.Value) return;

                            if (client.Aisling.GoldPoints + offer <= ServerSetup.Instance.Config.MaxCarryGold)
                            {
                                client.Aisling.GoldPoints += offer;
                                client.Aisling.Inventory.RemoveFromInventory(client, item);
                                client.SendAttributes(StatUpdateType.Primary);
                                client.SendAttributes(StatUpdateType.ExpGold);
                                client.PendingItemSessions = null;
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cMuch appreciated!");
                                TopMenu(client);
                            }
                        }
                    }
                    break;
                case 0x20:
                    {
                        client.PendingBuySessions = null;
                        client.PendingItemSessions = null;
                        client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
                    }
                    break;
            }

            break;
        }
    }

    private void ColorList(WorldClient client) => client.SendServerMessage(ServerMessageType.ScrollWindow, "Lavender = 0\n" +
                                                                 "Black = 1\n" +
                                                                 "Red = 2\n" +
                                                                 "Orange = 3\n" +
                                                                 "Blonde = 4\n" +
                                                                 "Cyan = 5\n" +
                                                                 "Blue = 6\n" +
                                                                 "Mulberry = 7\n" +
                                                                 "Olive = 8\n" +
                                                                 "Green = 9\n" +
                                                                 "Fire = 10\n" +
                                                                 "Brown = 11\n" +
                                                                 "Grey = 12\n" +
                                                                 "Navy = 13\n" +
                                                                 "Tan = 14\n" +
                                                                 "White = 15\n" +
                                                                 "Pink = 16\n" +
                                                                 "Chartreuse = 17\n" +
                                                                 "Golden = 18\n" +
                                                                 "Lemon = 19\n" +
                                                                 "Royal = 20\n" +
                                                                 "Platinum = 21\n" +
                                                                 "Lilac = 22\n" +
                                                                 "Fuchsia = 23\n" +
                                                                 "Magenta = 24\n" +
                                                                 "Peacock = 25\n" +
                                                                 "Neon Pink = 26\n" +
                                                                 "Arctic = 27\n" +
                                                                 "Mauve = 28\n" +
                                                                 "Neon Orange = 29\n" +
                                                                 "Sky = 30\n" +
                                                                 "Neon Green = 31\n" +
                                                                 "Pistachio = 32\n" +
                                                                 "Corn = 33\n" +
                                                                 "Cerulean = 34\n" +
                                                                 "Chocolate = 35\n" +
                                                                 "Ruby = 36\n" +
                                                                 "Hunter = 37\n" +
                                                                 "Crimson = 38\n" +
                                                                 "Ocean = 39\n" +
                                                                 "Ginger = 40\n" +
                                                                 "Mustard = 41\n" +
                                                                 "Apple = 42\n" +
                                                                 "Leaf = 43\n" +
                                                                 "Cobalt = 44\n" +
                                                                 "Strawberry = 45\n" +
                                                                 "Unusual = 46\n" +
                                                                 "Sea = 47\n" +
                                                                 "Harlequin = 48\n" +
                                                                 "Amethyst = 49\n" +
                                                                 "Neon Red = 50\n" +
                                                                 "Neon Yellow = 51\n" +
                                                                 "Rose = 52\n" +
                                                                 "Salmon = 53\n" +
                                                                 "Scarlet = 54\n" +
                                                                 "Honey = 55");
}
