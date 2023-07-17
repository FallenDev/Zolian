﻿using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
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

    public Banker(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        client.Aisling.Client.LoadBank();
        Refresh(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            new (0x11, "Deposit Item")
        };

        if (client.Aisling.BankManager.Items.Count > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x06, "Withdraw Item"));
        }

        if (client.Aisling.GoldPoints > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x07, "Deposit Gold"));
        }

        if (client.Aisling.BankedGold > 0)
        {
            options.Add(new Dialog.OptionsDataItem(0x08, "Withdraw Gold"));
        }

        client.SendOptionsDialog(Mundane, "We'll take real good care of your possessions.", options.ToArray());
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Bankable))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This item cannot be banked.");
            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "We can't accept that."));
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
            client.SendTextInput(Mundane, $"How many {client.PendingBankedSession.SelectedItem.Template.Name} would you like to deposit?\nCurrently have: {{=q{client.PendingBankedSession.SelectedItem.Stacks}{{=a, in this stack.");
            // Sets that the player is now committing to a stacked deposit
            client.PendingBankedSession.DepositStackedItem = true;
        }
        else
        {
            var options = new List<Dialog.OptionsDataItem>
            {
                new (0x0051, "Confirm"),
                new (0x0052, "Cancel")
            };

            client.SendOptionsDialog(Mundane, $"I can hold {{=c{client.PendingBankedSession.SelectedItem.DisplayName}{{=a, But it'll cost you {{=q{client.PendingBankedSession.Cost}{{=a gold.", options.ToArray());
        }
    }

    public override void OnGoldDropped(WorldClient client, uint money)
    {
        if (money >= 1)
        {
            Refresh(client);
            client.PendingBankedSession.TempGold = money;
            OnResponse(client, 0x02, null);
        }
        else
        {
            client.SendOptionsDialog(Mundane, "I'm sorry, where is it?");
        }
    }

    public override async void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        client.Aisling.Client.LoadBank();

        switch (responseID)
        {
            // Item Click from inventory when depositing
            case 0x0800:
            {
                // Makes sure the trade is fresh
                Refresh(client);
                // Sets Inventory Slot
                client.PendingBankedSession.InventorySlot = Convert.ToByte(args);
                // Sets Selected Item
                client.Aisling.Inventory.Items.TryGetValue(client.PendingBankedSession.InventorySlot, out var value);

                if (value == null) return;
                client.PendingBankedSession.SelectedItem = value;
                // Calculates and stores the cost to bank the item
                var cost = (uint)(client.PendingBankedSession.SelectedItem.Template.Value > 1000 ? client.PendingBankedSession.SelectedItem.Template.Value / 30 : 128);
                client.PendingBankedSession.Cost = cost;

                if (client.PendingBankedSession.SelectedItem.Template.CanStack && client.PendingBankedSession.SelectedItem.Stacks >= 1)
                {
                    client.SendTextInput(Mundane, $"How many {client.PendingBankedSession.SelectedItem.Template.Name} would you like to deposit?\n" + 
                                                  $"Currently have: {{=q{client.PendingBankedSession.SelectedItem.Stacks}{{=a, in this stack.");
                            
                    // Sets that the player is now committing to a stacked deposit
                    client.PendingBankedSession.DepositStackedItem = true;
                }
                else
                {
                    var options = new List<Dialog.OptionsDataItem>
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
                            var options = new List<Dialog.OptionsDataItem>
                            {
                                new (0x02, "Yes"),
                                new (0x07, "No")
                            };

                            client.SendOptionsDialog(Mundane, $"Ok, so you'd like me to hold onto {{=c{client.PendingBankedSession.TempGold}{{=a coins for you?\nCurrent Interest rate is:{{=q 0.00777", options.ToArray());
                        }
                        else
                        {
                            client.SendServerMessage(ServerMessageType.OrangeBar1, "You don't have enough gold.");
                            client.PendingBankedSession.DepositGold = false;
                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"{{=cEh, that's not quite right. Let's try again.{{=a\nInventory: {{=c{client.Aisling.GoldPoints}\n{{=aBanked: {{=q{client.Aisling.BankedGold}", args,
                            new Dialog.OptionsDataItem(0x07, "Alright"));
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
                            var options = new List<Dialog.OptionsDataItem>
                            {
                                new (0x03, "Yes"),
                                new (0x07, "No")
                            };

                            client.SendOptionsDialog(Mundane, $"So, you'd like me to return {{=c{client.PendingBankedSession.TempGold}{{=a gold?", options.ToArray());
                        }

                        if (client.Aisling.GoldPoints + client.PendingBankedSession.TempGold >= 1000000000)
                        {
                            client.SendServerMessage(ServerMessageType.OrangeBar1, "I'm sorry, you're not able to hold that much.");
                            client.PendingBankedSession.WithdrawGold = false;
                            client.CloseDialog();
                        }

                        if (client.PendingBankedSession.TempGold > client.Aisling.BankedGold)
                        {
                            client.SendServerMessage(ServerMessageType.OrangeBar1, "You haven't invested that much with us.");
                            client.PendingBankedSession.WithdrawGold = false;
                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, $"{{=cEh, that's not quite right. Let's try again.{{=a\nInventory: {{=c{client.Aisling.GoldPoints}\n{{=aBanked: {{=q{client.Aisling.BankedGold}", args,
                            new Dialog.OptionsDataItem(0x08, "Alright"));
                    }
                }

                // Object Handling
                if (string.IsNullOrEmpty(args))
                {
                    OnClick(client, Mundane.Serial);
                    return;
                }

                // Set Qty if stacked
                ushort.TryParse(args, out var amount);
                if (amount == 0) amount = 1;

                client.PendingBankedSession.ArgsQuantity = amount;

                if (client.PendingBankedSession.DepositStackedItem)
                {
                    if (client.PendingBankedSession.SelectedItem == null) return;
                    if (client.PendingBankedSession.SelectedItem.Stacks > 1)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > client.PendingBankedSession.SelectedItem.Stacks)
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "You don't have that many in your inventory."));
                            OnClick(client, Mundane.Serial);
                            return;
                        }
                    }
                            
                    client.PendingBankedSession.Cost *= amount;

                    var opts = new List<Dialog.OptionsDataItem>
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
                        client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Uh.. Let us check out ledger. Nope, not here."));
                        OnClick(client, Mundane.Serial);
                        return;
                    }

                    if (client.PendingBankedSession.ArgsQuantity >= 1)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > client.PendingBankedSession.SelectedItem.Stacks)
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "It seems you don't have that many with us."));
                            OnClick(client, Mundane.Serial);
                            return;
                        }
                    }

                    #endregion

                    var withdraw = await client.Aisling.BankManager.Withdraw(client, Mundane);

                    if (withdraw)
                    {
                        if (client.PendingBankedSession.ArgsQuantity > 1)
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here are your stacks of {client.PendingBankedSession.SelectedItem.Template.Name}."));
                        }
                        else
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, here is your {client.PendingBankedSession.SelectedItem.Template.Name}."));
                        }

                        client.SendAttributes(StatUpdateType.WeightGold);
                        OnClick(client, Mundane.Serial);
                    }
                    else
                    {
                        OnClick(client, Mundane.Serial);
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
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aYou withdrew {{=c{client.PendingBankedSession.TempGold}{{=a coins out of {{=q{client.Aisling.BankedGold}{{=a currently banked.");
                OnClick(client, Mundane.Serial);
            }
                break;
            case 0x07:
            {
                client.SendTextInput(Mundane, $"Inventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}");
                client.PendingBankedSession.DepositGold = true;
            }
                break;
            case 0x08:
            {
                client.SendTextInput(Mundane, $"How much would you like returned?\nInventory: {{=q{client.Aisling.GoldPoints}\n{{=aBanked: {{=c{client.Aisling.BankedGold}");
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
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"Great, That will be {client.PendingBankedSession.Cost} gold."));
                            client.PendingBankedSession.DepositStackedItem = false;
                            CompleteTrade(client, client.PendingBankedSession.Cost);
                        }
                        else
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "We can't accept that. I'm sorry."));
                            client.CloseDialog();
                        }
                    }
                    else
                    {
                        client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Come back when you have enough gold."));
                        client.PendingBankedSession.DepositStackedItem = false;
                        client.CloseDialog();
                    }
                }
                else
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, "Well? Where is it?"));
                    client.PendingBankedSession.DepositStackedItem = false;
                    OnClick(client, Mundane.Serial);
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
                    OnClick(client, Mundane.Serial);
                    return;
                }

                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(client.PendingBankedSession.SelectedItem.Template.Name)) TopMenu(client);

                switch (client.PendingBankedSession.SelectedItem.Template.CanStack)
                {
                    case true:
                        client.SendTextInput(Mundane, $"How many {{=q{client.PendingBankedSession.SelectedItem.Template.Name}{{=a would you like back?");
                        client.PendingBankedSession.WithdrawItem = true;
                        break;
                    case false:
                    {
                        var withdraw = await client.Aisling.BankManager.Withdraw(client, Mundane);

                        if (withdraw)
                        {
                            client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{client.Aisling.Username}, Here is your {args}."));
                            client.SendAttributes(StatUpdateType.WeightGold);
                            OnClick(client, Mundane.Serial);
                        }
                        else
                        {
                            OnClick(client, Mundane.Serial);
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
                OnClick(client, Mundane.Serial);
            }
                break;
            default:
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{Mundane.Template.Name} waves you away.");
                client.CloseDialog();
            }
                break;
        }
    }
        
    private void CompleteTrade(WorldClient client, uint cost)
    {
        client.Aisling.GoldPoints -= cost;
        client.Aisling.BankManager.UpdatePlayersWeight(client);
        client.SendAttributes(StatUpdateType.WeightGold);
        OnClick(client, Mundane.Serial);
    }

    private void DepositMenu(WorldClient client)
    {
        //if (client.Aisling.Inventory.BankList.Any())
        //    client.Send(new ServerFormat2F(Mundane, "We'll take care of your possessions.",
        //        new BankingData(0x08, client.Aisling.Inventory.BankList)));
        //else
        //    OnClick(client, Mundane.Serial);
    }

    private void WithDrawMenu(WorldClient client)
    {
        //if (client.Aisling.BankManager.Items.Count > 0)
        //    client.Send(new ServerFormat2F(Mundane, "What would you like back?",
        //        new WithdrawBankData(0x0A, client.Aisling.BankManager)));
        //else
        //    OnClick(client, Mundane.Serial);
    }

    private static void Refresh(IWorldClient client)
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