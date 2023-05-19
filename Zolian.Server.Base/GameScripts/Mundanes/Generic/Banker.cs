using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Banker")]
public class Banker : MundaneScript
{
    private readonly Bank _bank = new();

    public Banker(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        if (Mundane.WithinEarShotOf(client.Aisling))
        {
            TopMenu(client);
        }
    }

    public override void TopMenu(IGameClient client)
    {
        client.Aisling.Client.LoadBank();
        Refresh(client);

        var options = new List<OptionsDataItem>
        {
            new (0x11, "Deposit Item")
        };

        if (client.Aisling.BankManager.Items.Count > 0)
        {
            options.Add(new OptionsDataItem(0x06, "Withdraw Item"));
        }

        if (client.Aisling.GoldPoints > 0)
        {
            options.Add(new OptionsDataItem(0x07, "Deposit Gold"));
        }

        if (client.Aisling.BankedGold > 0)
        {
            options.Add(new OptionsDataItem(0x08, "Withdraw Gold"));
        }

        client.SendOptionsDialog(Mundane, "We'll take real good care of your possessions.", options.ToArray());
    }

    public override void OnItemDropped(GameClient client, Item item)
    {
        if (item == null) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Bankable))
        {
            client.SendMessage(0x03, "This item cannot be banked.");
            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
            {
                Serial = Mundane.Serial,
                Text = "We can't accept that.",
                Type = 0x03
            });

            return;
        }

        Refresh(client);
        client.PendingBankedSession.InventorySlot = item.InventorySlot;
        client.PendingBankedSession.SelectedItem = item;

        // Calculates and stores the cost to bank the item
        var cost = (uint)(client.PendingBankedSession.SelectedItem.Template.Value > 1000 ? client.PendingBankedSession.SelectedItem.Template.Value / 30 : 128);
        client.PendingBankedSession.Cost = cost;

        if (client.PendingBankedSession.SelectedItem.Template.CanStack && client.PendingBankedSession.SelectedItem.Stacks > 1)
        {
            client.Send(new ServerFormat2F(Mundane, $"How many {client.PendingBankedSession.SelectedItem.Template.Name} would you like to deposit?\nCurrently have: {{=q{client.PendingBankedSession.SelectedItem.Stacks}{{=a, in this stack.", new TextInputData()));
            // Sets that the player is now committing to a stacked deposit
            client.PendingBankedSession.DepositStackedItem = true;
        }
        else
        {
            var options = new List<OptionsDataItem>
            {
                new (0x0051, "Confirm"),
                new (0x0052, "Cancel")
            };

            client.SendOptionsDialog(Mundane, $"I can hold {{=c{client.PendingBankedSession.SelectedItem.DisplayName}{{=a, But it'll cost you {{=q{client.PendingBankedSession.Cost}{{=a gold.", options.ToArray());
        }
    }

    public override void OnGoldDropped(GameClient client, uint money)
    {
        if (money >= 1)
        {
            Refresh(client);
            client.PendingBankedSession.TempGold = money;
            OnResponse(client.Server, client, 0x02, null);
        }
        else
        {
            client.SendOptionsDialog(Mundane, "I'm sorry, where is it?");
        }
    }

    public override async void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        if (!Mundane.WithinEarShotOf(client.Aisling)) return;

        client.Aisling.Client.LoadBank();

        switch (responseID)
        {
            // Item Click from inventory when depositing
            case 0x0800:
            {
                // Makes sure the trade is fresh
                Refresh(client);
                // Sets Inventory Slot
                client.PendingBankedSession.InventorySlot = Convert.ToInt32(args);
                // Sets Selected Item
                client.Aisling.Inventory.Items.TryGetValue(client.PendingBankedSession.InventorySlot, out var value);

                if (value == null) return;
                client.PendingBankedSession.SelectedItem = value;
                // Calculates and stores the cost to bank the item
                var cost = (uint)(client.PendingBankedSession.SelectedItem.Template.Value > 1000 ? client.PendingBankedSession.SelectedItem.Template.Value / 30 : 128);
                client.PendingBankedSession.Cost = cost;

                if (client.PendingBankedSession.SelectedItem.Template.CanStack && client.PendingBankedSession.SelectedItem.Stacks >= 1)
                {
                    client.Send(new ServerFormat2F(Mundane, $"How many {client.PendingBankedSession.SelectedItem.Template.Name} would you like to deposit?\n" +
                                                            $"Currently have: {{=q{client.PendingBankedSession.SelectedItem.Stacks}{{=a, in this stack.", new TextInputData()));
                            
                    // Sets that the player is now committing to a stacked deposit
                    client.PendingBankedSession.DepositStackedItem = true;
                }
                else
                {
                    var options = new List<OptionsDataItem>
                    {
                        new (0x0051, "Confirm"),
                        new (0x0052, "Cancel")
                    };

                    client.SendOptionsDialog(Mundane, $"I can hold {{=c{client.PendingBankedSession.SelectedItem.DisplayName}{{=a, But it'll cost you {{=q{client.PendingBankedSession.Cost}{{=a gold.", options.ToArray());
                }
            }
                break;
            // Set Amount NPC Input within an NPC Reply
            case 0x0000:
            {
                // Bank Gold
                if (client.PendingBankedSession.DepositGold)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(args, "^[0-9]*$"))
                    {
                        try
                        {
                            client.PendingBankedSession.TempGold = Convert.ToUInt32(args);
                        }
                        catch (OverflowException e)
                        {
                            Crashes.TrackError(e);
                            ServerSetup.Logger($"Bank overflow: {client.Aisling.Username}");
                            client.CloseDialog();
                            return;
                        }

                        if (client.Aisling.GoldPoints >= client.PendingBankedSession.TempGold)
                        {
                            var options = new List<OptionsDataItem>
                            {
                                new (0x02, "Yes"),
                                new (0x07, "No")
                            };

                            client.SendOptionsDialog(Mundane, $"Ok, so you'd like me to hold onto {{=c{client.PendingBankedSession.TempGold}{{=a coins for you?\nCurrent Interest rate is:{{=q 0.00777", options.ToArray());
                        }
                        else
                        {
                            client.SendMessage(0x02, "You don't have enough gold.");
                            client.PendingBankedSession.DepositGold = false;
                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"{{=cEh, that's not quite right. Let's try again.{{=a\nInventory: {{=c{client.Aisling.GoldPoints}\n{{=aBanked: {{=q{client.Aisling.BankedGold}", args,
                            new OptionsDataItem(0x07, "Alright"));
                    }
                }

                if (client.PendingBankedSession.WithdrawGold)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(args, "^[0-9]*$"))
                    {
                        try
                        {
                            client.PendingBankedSession.TempGold = Convert.ToUInt32(args);
                        }
                        catch (OverflowException e)
                        {
                            Crashes.TrackError(e);
                            ServerSetup.Logger($"Bank overflow: {client.Aisling.Username}");
                            client.CloseDialog();
                            return;
                        }

                        if (client.Aisling.BankedGold >= client.PendingBankedSession.TempGold && client.Aisling.GoldPoints + client.PendingBankedSession.TempGold <= 100000000)
                        {
                            var options = new List<OptionsDataItem>
                            {
                                new (0x03, "Yes"),
                                new (0x07, "No")
                            };

                            client.SendOptionsDialog(Mundane, $"So, you'd like me to return {{=c{client.PendingBankedSession.TempGold}{{=a gold?", options.ToArray());
                        }

                        if (client.Aisling.GoldPoints + client.PendingBankedSession.TempGold >= 1000000000)
                        {
                            client.SendMessage(0x02, "I'm sorry, you're not able to hold that much.");
                            client.PendingBankedSession.WithdrawGold = false;
                            client.CloseDialog();
                        }

                        if (client.PendingBankedSession.TempGold > client.Aisling.BankedGold)
                        {
                            client.SendMessage(0x02, "You haven't invested that much with us.");
                            client.PendingBankedSession.WithdrawGold = false;
                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"{{=cEh, that's not quite right. Let's try again.{{=a\nInventory: {{=c{client.Aisling.GoldPoints}\n{{=aBanked: {{=q{client.Aisling.BankedGold}", args,
                            new OptionsDataItem(0x08, "Alright"));
                    }
                }

                // Object Handling
                if (string.IsNullOrEmpty(args))
                {
                    OnClick(server, client);
                    return;
                }

                // Set Qty if stacked
                uint.TryParse(args, out var amount);
                if (amount == 0) amount = 1;

                client.PendingBankedSession.ArgsQuantity = (int)amount;

                if (client.PendingBankedSession.DepositStackedItem)
                {
                    if (client.PendingBankedSession.SelectedItem == null) return;
                    if (client.PendingBankedSession.SelectedItem.Stacks > 1)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > client.PendingBankedSession.SelectedItem.Stacks)
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = "You don't have that many in your inventory.",
                                Type = 0x03
                            });

                            OnClick(server, client);
                            return;
                        }
                    }
                            
                    client.PendingBankedSession.Cost *= amount;

                    var opts = new List<OptionsDataItem>
                    {
                        new (0x0051, "Yes"),
                        new (0x0052, "No")
                    };

                    client.SendOptionsDialog(Mundane, $"It will cost you a total of {{=c{client.PendingBankedSession.Cost}{{=a coins to deposit {{=q{client.PendingBankedSession.ArgsQuantity} {{=e{client.PendingBankedSession.SelectedItem.Template.Name}{{=a, proceed?", opts.ToArray());

                }

                if (client.PendingBankedSession.ArgsQuantity > 0 && client.PendingBankedSession.WithdrawItem)
                {
                    #region Checks

                    if (client.PendingBankedSession.SelectedItem == null)
                    {
                        Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                        {
                            Serial = Mundane.Serial,
                            Text = "Uh.. Let us check our ledger. Nope, not here.",
                            Type = 0x03
                        });

                        OnClick(server, client);
                        return;
                    }

                    if (client.PendingBankedSession.ArgsQuantity >= 1)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > client.PendingBankedSession.SelectedItem.Stacks)
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = "You don't have that many with us.",
                                Type = 0x03
                            });

                            OnClick(server, client);
                            return;
                        }
                    }

                    #endregion

                    var withdraw = await client.Aisling.BankManager.Withdraw(client, Mundane);

                    if (withdraw)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > 1)
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = $"{client.Aisling.Username}, here are your stacks of {client.PendingBankedSession.SelectedItem.Template.Name}.",
                                Type = 0x03
                            });
                        }
                        else
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = $"{client.Aisling.Username}, Here is your {client.PendingBankedSession.SelectedItem.Template.Name}.",
                                Type = 0x03
                            });
                        }

                        client.SendStats(StatusFlags.WeightMoney);
                        OnClick(server, client);
                    }
                    else
                    {
                        OnClick(server, client);
                    }
                }
            }
                break;

            #region Gold Handling

            case 0x02:
            {
                _bank.DepositGold(client, client.PendingBankedSession.TempGold);
                client.PendingBankedSession.DepositGold = false;
                client.SendOptionsDialog(Mundane, $"You've deposited {{=c{client.PendingBankedSession.TempGold}{{=a coins out of {{=q{client.Aisling.BankedGold}{{=a currently banked.");
            }
                break;
            case 0x03:
            {
                _bank.WithdrawGold(client, client.PendingBankedSession.TempGold);
                client.PendingBankedSession.WithdrawGold = false;
                client.SendMessage(0x0C, $"{{=aYou withdrew {{=c{client.PendingBankedSession.TempGold}{{=a coins out of {{=q{client.Aisling.BankedGold}{{=a currently banked.");
                OnClick(server, client);
            }
                break;
            case 0x07:
            {
                client.Send(new ServerFormat2F(Mundane, $"Inventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}", new TextInputData()));
                client.PendingBankedSession.DepositGold = true;
            }
                break;
            case 0x08:
            {
                client.Send(new ServerFormat2F(Mundane, $"How much would you like returned?\nInventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}", new TextInputData()));
                client.PendingBankedSession.WithdrawGold = true;
            }
                break;

            #endregion

            #region Deposit Handling

            case 0x11:
            {
                DepositMenu(client);
            }
                break;
            case 0x51:
            {
                // depositing items 'fae wing' for an example will deposit as 0 instead of 1 
                // if you deposit more than 1 you can pull out, however you cannot stack more than 1 group 
                // regular items that aren't stacked items will pull out and deposit no problem
                // stacked items have issues when trying to deposit 1 or deposit multiple stacks. (perhaps put logic in to only hold one stack)
                // stored procedure that looks for the name and if exists, say sorry. no can do. 
                // might also be beneficial to force stacks of 1 that can be stacked as 1 instead of showing up as 0. 
                if (client.PendingBankedSession.SelectedItem.Stacks >= (ushort)client.PendingBankedSession.ArgsQuantity)
                {
                    if (client.Aisling.GoldPoints >= client.PendingBankedSession.Cost)
                    {
                        client.Aisling.Inventory.RemoveRange(client, client.PendingBankedSession.SelectedItem,
                            client.PendingBankedSession.SelectedItem.Stacks == 1
                                ? 1
                                : client.PendingBankedSession.ArgsQuantity);

                        var deposited = await _bank.Deposit(client, client.PendingBankedSession.SelectedItem);

                        if (deposited)
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = $"Great, That will be {client.PendingBankedSession.Cost} gold.",
                                Type = 0x03
                            });

                            client.PendingBankedSession.DepositStackedItem = false;
                            CompleteTrade(client, client.PendingBankedSession.Cost);
                        }
                        else
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = "We won't accept that. I'm sorry.",
                                Type = 0x03
                            });

                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                        {
                            Serial = Mundane.Serial,
                            Text = "Come back when you have enough gold.",
                            Type = 0x03
                        });

                        client.PendingBankedSession.DepositStackedItem = false;
                        client.CloseDialog();
                    }
                }
                else
                {
                    Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                    {
                        Serial = Mundane.Serial,
                        Text = "Well, where is it?",
                        Type = 0x03
                    });

                    client.PendingBankedSession.DepositStackedItem = false;
                    OnClick(server, client);
                }
            }
                break;

            #endregion

            #region Withdraw Handling

            case 0x06:
            {
                WithDrawMenu(client);
            }
                break;
            case 0x0A:
            {
                if (string.IsNullOrEmpty(args)) return;

                var bankKeys = client.Aisling.BankManager.Items.Keys;

                foreach (var key in bankKeys.Where(key => key != 0))
                {
                    if (!client.Aisling.BankManager.Items.TryGetValue(key, out var itemToKey)) continue;
                    if (args != itemToKey.NoColorDisplayName) continue;
                    client.PendingBankedSession.SelectedItem = itemToKey;
                    client.PendingBankedSession.ItemId = itemToKey.ItemId;
                    break;
                }

                if (client.PendingBankedSession.SelectedItem == null)
                {
                    OnClick(server, client);
                    return;
                }

                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(client.PendingBankedSession.SelectedItem.Template.Name)) TopMenu(client);

                switch (client.PendingBankedSession.SelectedItem.Template.CanStack)
                {
                    case true:
                        client.Send(new ServerFormat2F(Mundane, $"How many {{=q{client.PendingBankedSession.SelectedItem.Template.Name}{{=a would you like back?", new TextInputData()));
                        client.PendingBankedSession.WithdrawItem = true;
                        break;
                    case false:
                    {
                        var withdraw = await client.Aisling.BankManager.Withdraw(client, Mundane);

                        if (withdraw)
                        {
                            Mundane.Show(Scope.NearbyAislings, new ServerFormat0D
                            {
                                Serial = Mundane.Serial,
                                Text = $"{client.Aisling.Username}, Here is your {args}.",
                                Type = 0x03
                            });

                            client.SendStats(StatusFlags.WeightMoney);
                            OnClick(server, client);
                        }
                        else
                        {
                            OnClick(server, client);
                        }
                        break;
                    }
                }

                client.Aisling.BankManager.UpdatePlayersWeight(client);
            }
                break;

            #endregion

            case 0x52:
            {
                OnClick(server, client);
            }
                break;
            default:
            {
                client.SendMessage(0x02, $"{Mundane.Template.Name} waves you away.");
                client.CloseDialog();
            }
                break;
        }
    }
        
    private void CompleteTrade(GameClient client, uint cost)
    {
        client.Aisling.GoldPoints -= cost;
        client.Aisling.BankManager.UpdatePlayersWeight(client);
        client.SendStats(StatusFlags.WeightMoney);
        OnClick(Server, client);
    }

    private void DepositMenu(GameClient client)
    {
        if (client.Aisling.Inventory.BankList.Any())
            client.Send(new ServerFormat2F(Mundane, "We'll take care of your possessions.",
                new BankingData(0x08, client.Aisling.Inventory.BankList)));
        else
            OnClick(Server, client);
    }

    private void WithDrawMenu(GameClient client)
    {
        if (client.Aisling.BankManager.Items.Count > 0)
            client.Send(new ServerFormat2F(Mundane, "What would you like back?",
                new WithdrawBankData(0x0A, client.Aisling.BankManager)));
        else
            OnClick(Server, client);
    }

    private static void Refresh(IGameClient client)
    {
        client.PendingBankedSession = new PendingBanked()
        {
            SelectedItem = null,
            InventorySlot = 0,
            ArgsQuantity = 0,
            BankQuantity = 0,
            Cost = 0,
            TempGold = 0,
            DepositGold = false,
            DepositStackedItem = false,
            WithdrawGold = false,
            WithdrawItem = false
        };
    }
}