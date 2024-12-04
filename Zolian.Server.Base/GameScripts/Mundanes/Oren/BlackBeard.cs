using Darkages.Common;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Oren;

[Script("Black Beard")]
public class BlackBeard(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.PendingBuySessions = null;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>
        {
            new (0x02, "Buy"),
            new (0x04, "My Treasure")
        };

        client.SendOptionsDialog(Mundane, "Ah adventurer, are ye here for my treasure? Too late! I lost it somewhere in the ruins.", opts.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

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
                    }
                }
                break;
            case 0x01: // Follows Sequence to buy a stacked item from the vendor
                var containsInt = ushort.TryParse(args, out var amount);
                if (containsInt)
                {
                    if (client.PendingBuySessions == null)
                    {
                        client.SendOptionsDialog(Mundane, "Scram!");
                        return;
                    }

                    if (client.PendingBuySessions != null)
                    {
                        client.PendingBuySessions.Quantity = amount;
                        NpcShopExtensions.BuyStackedItemFromInventory(client, Mundane);
                    }
                }
                break;
            case 0x02:
                client.SendItemShopDialog(Mundane, "So you want meh shovel?", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x04:
                {
                    client.SendOptionsDialog(Mundane, "Arggh when I'm ready to talk about it. I will!");
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
                                itemCreated = itemCreated.Create(client.Aisling, template,
                                    NpcShopExtensions.DungeonLowQuality(), ItemQualityVariance.DetermineVariance(),
                                    ItemQualityVariance.DetermineWeaponVariance());
                                var given = itemCreated.GiveTo(client.Aisling);
                                if (!given)
                                {
                                    client.Aisling.BankManager.Items.TryAdd(itemCreated.ItemId, itemCreated);
                                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
                                }
                            }
                            client.SendAttributes(StatUpdateType.Primary);
                            client.SendAttributes(StatUpdateType.ExpGold);
                            client.PendingBuySessions = null;
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cFind my Treasure!");
                            TopMenu(client);
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "Well? Where is it?");
                            client.PendingBuySessions = null;
                        }
                    }
                }
                break;
            case 0x20:
                {
                    client.PendingBuySessions = null;
                    client.SendOptionsDialog(Mundane, ServerSetup.Instance.Config.MerchantCancelMessage);
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item item) { }
}