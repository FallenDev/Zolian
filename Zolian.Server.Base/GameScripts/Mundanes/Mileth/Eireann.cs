using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;
using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Eireann")]
public class Eireann(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.QuestManager.EternalLove)
        {
            case false when (client.Aisling.Level >= 50):
                options.Add(new(0x06, "{=qEternal Love"));
                break;
            case true when (!client.Aisling.QuestManager.CryptTerrorSlayed):
                options.Add(new(0x08, "..."));
                break;
        }

        options.Add(new(0x07, "Rumors"));
        options.Add(new(0x02, "Buy"));
        options.Add(new(0x03, "Pawn"));

        client.SendOptionsDialog(Mundane, "Greetings Adventurer, care for some mead?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var gossip = Random.Shared.Next(1, 6);

        switch (responseID)
        {
            case 0x00:
                {
                    if (string.IsNullOrEmpty(args)) return;
                    var itemOrSlot = ushort.TryParse(args, out var slot);

                    switch (itemOrSlot)
                    {
                        // Buying
                        case false:
                            NpcShopExtensions.BuyItemFromInventory(client, Mundane, args);
                            break;
                        // Selling
                        case true when slot > 0:
                            NpcShopExtensions.SellItemFromInventory(client, Mundane, args);
                            break;
                    }
                }
                break;
            case 0x01: // Follows Sequence to buy a stacked item from the vendor
                var containsInt = ushort.TryParse(args, out var amount);
                if (containsInt)
                {
                    if (client.PendingBuySessions == null && client.PendingItemSessions == null)
                    {
                        client.SendOptionsDialog(Mundane, "I'm sorry, freshly sold out.");
                        return;
                    }

                    if (client.PendingBuySessions != null)
                    {
                        client.PendingBuySessions.Quantity = amount;
                        NpcShopExtensions.BuyStackedItemFromInventory(client, Mundane);
                    }

                    if (client.PendingItemSessions != null)
                    {
                        client.PendingItemSessions.Quantity = amount;
                        NpcShopExtensions.SellStackedItemFromInventory(client, Mundane);
                    }
                }
                break;
            case 0x02:
                {
                    client.SendItemShopDialog(Mundane, "We have a decent selection of craft.", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                }
                break;
            case 0x03:
                {
                    client.SendItemSellDialog(Mundane, "What do you want to pawn?", NpcShopExtensions.GetCharacterSellInventoryByteList(client));
                }
                break;
            case 0x06:
            {
                var options = new List<Dialog.OptionsDataItem>();

                switch (client.Aisling.Gender)
                {
                    case Gender.Male:
                        options.Add(new(0x0A, "I have a lady friend whom might be able to help"));
                        break;
                    case Gender.Female:
                        options.Add(new(0x0A, "A good friend of mine can help"));
                        break;
                }

                options.Add(new(0x09, "I'm sorry for their loss"));
                client.SendOptionsDialog(Mundane, "I have a friend who is grieving the lost of their loved one. They died tragically in the last great goblin war.", options.ToArray());
            }
                break;
            case 0x07:
                {
                    switch (gossip)
                    {
                        case 1:
                            client.SendOptionsDialog(Mundane, "You can recover from wounds by laying down (Clicking a {=qBed{=a).");
                            break;
                        case 2:
                            client.SendOptionsDialog(Mundane, "Not all quests offer rewards that you can see, some times it's good to just help people.");
                            break;
                        case 3:
                            client.SendOptionsDialog(Mundane, "I overheard that there is a secret shop in Abel... I wonder where?");
                            client.Aisling.QuestManager.AbelShopAccess = true;
                            break;
                        case 4:
                            client.SendOptionsDialog(Mundane, "There are three basement access panels to the crypts.");
                            break;
                        case 5:
                            client.SendOptionsDialog(Mundane, "I heard a rumor that the altar can give you {=bForsaken{=a items if you're really lucky.");
                            break;
                    }
                }
                break;
            case 0x08:
                {
                    client.SendOptionsDialog(Mundane, "That drunk, just keeps going on about some terror.");
                }
                break;
            case 0x09:
                client.CloseDialog();
                break;
            case 0x0A:
                // ToDo: Finish "The Letter" questline
                client.CloseDialog();
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
                                itemCreated = itemCreated.Create(client.Aisling, template,
                                    NpcShopExtensions.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(),
                                    ItemQualityVariance.DetermineWeaponVariance());
                                itemCreated.GiveTo(client.Aisling);
                            }
                            client.SendAttributes(StatUpdateType.Primary);
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.PendingBuySessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
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
                        var item = client.Aisling.Inventory.Get(i => i != null && i.ItemId == client.PendingItemSessions.ID).First();

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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you!");
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
            case 0x30:
                {
                    if (client.PendingItemSessions != null)
                    {
                        NpcShopExtensions.CompletePendingItemSell(client, Mundane);
                    }

                    TopMenu(client);
                }
                break;
            case 0x500:
                {
                    NpcShopExtensions.SellItemDroppedFromInventory(client, Mundane, args);
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
            OnResponse(client, 0x500, item.InventorySlot.ToString());
        }
    }
}