using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("MinerGame")]
public class Miner : MundaneScript
{
    private readonly List<SpellTemplate> _spellList;

    public Miner(WorldServer server, Mundane mundane) : base(server, mundane)
    {
        _spellList = ObtainSpellList();
    }

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
            new(0x01, "Pickaxe?"),
            new(0x02, "How do I mine?")
        };

        if (!client.Aisling.SpellBook.HasSpell("Mine") && _spellList.Count > 0)
            opts.Add(new Dialog.OptionsDataItem(0x03, "Please teach me how to mine"));

        client.SendOptionsDialog(Mundane, "Ahh, so you heard? The walls of this cavern are filled with wonders!", opts.ToArray());
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
            case 0x01:
                client.SendItemShopDialog(Mundane, "Here's what I have to offer.", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x02:
                {
                    client.SendOptionsDialog(Mundane, $"Very easy, after you have learned my techniques. You then buy my -very- cheap pickaxe.\n" +
                                                      "You then walk around while holding the pickaxe and cast the mining techniques I've shown you.");
                    break;
                }
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
            case 0x03:
                {
                    var learnedSkills = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSkills = _spellList.Except(learnedSkills).ToList();

                    if (newSkills.Count > 0)
                    {
                        client.SendSpellLearnDialog(Mundane, "Mining is special, as you walk you can mine and if you find something you'll know!", 0x04, newSkills);
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "You must have already learned how to mine from me.");
                    }

                    break;
                }
            case 0x04:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;

                    var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
                    {
                        if (!result)
                        {
                            client.SendOptionsDialog(Mundane, msg, subject.Name);
                        }
                    });

                    if (conditions)
                    {
                        client.SendOptionsDialog(Mundane, "Have you brought what is required?",
                            subject.Name,
                            new Dialog.OptionsDataItem(0x05, "Yes, I have your gold"),
                            new Dialog.OptionsDataItem(0x06, "No, I'm poor"));
                    }

                    break;
                }
            case 0x05:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;
                    client.LearnSpell(Mundane, subject, "Remember, you must have your pickaxe equipped to mine.");

                    break;
                }
            case 0x06:
                {
                    client.CloseDialog();
                    break;
                }
        }
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm sorry I don't buy items.");
        }
    }
}