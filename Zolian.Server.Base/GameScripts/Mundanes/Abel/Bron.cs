using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Bron")]
public class Bron : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;

    public Bron(WorldServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
    }

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
            new(0x0017, "Gear & Goods")
        };

        if (_skillList.Count > 0)
        {
            options.Add(new(0x0016, "Show Available Skills"));
        }

        options.Add(new(0x02, "Forget Skill"));

        client.SendOptionsDialog(Mundane,
            client.Aisling.Stage <= ClassStage.Master
                ? "Thrust until you hit bone."
                : "Ahh, what can I do for you?", options.ToArray());
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
                    if (client.PendingBuySessions == null && client.PendingItemSessions == null)
                    {
                        client.SendOptionsDialog(Mundane, "I no longer have the item");
                        return;
                    }

                    if (client.PendingBuySessions != null)
                    {
                        client.PendingBuySessions.Quantity = amount;
                        NpcShopExtensions.BuyStackedItemFromInventory(client, Mundane);
                    }
                }
                break;

            #region Skills

            case 0x0016:
                {
                    var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSkills = _skillList.Except(learnedSkills).Where(i => i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.Path)
                                                                                || i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.Path)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                                                                                || i.Prerequisites.ClassRequired.ClassFlagIsSet(Class.Peasant)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(Class.Peasant)).ToList();

                    newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSkills.Count > 0)
                    {
                        client.SendSkillLearnDialog(Mundane, "What move do you wish to learn? \nThese skills have been taught for generations now and are available to you.", 0x0003, newSkills);
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
                    }

                    break;
                }
            case 0x0002:
                {
                    client.SendForgetSkills(Mundane,
                        "Muscle memory is a hard thing to unlearn. \nYou may come back to relearn what the mind has lost but the muscle still remembers.", 0x9000);
                    break;
                }
            case 0x9000:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "You don't quite have that skill.");
                        client.CloseDialog();
                    }

                    client.Aisling.SkillBook.Remove(client, (byte)idx);
                    client.LoadSkillBook();

                    client.SendForgetSkills(Mundane, "Your body is still, breathing in, relaxed. \nAny other skills you wish to forget?", 0x9000);
                    break;
                }
            case 0x0003:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x0006, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x0004, "Learn"),
                        new Dialog.OptionsDataItem(0x0001, "No, thank you."));
                    break;
                }
            case 0x0004:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
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
                            new Dialog.OptionsDataItem(0x0005, "Yes."),
                            new Dialog.OptionsDataItem(0x0001, "I'll come back later."));
                    }

                    break;
                }
            case 0x0006:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                    if (subject == null) return;

                    client.SendOptionsDialog(Mundane,
                        $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                        subject.Name,
                        new Dialog.OptionsDataItem(0x0004, "Yes"),
                        new Dialog.OptionsDataItem(0x0001, "No"));

                    break;
                }
            case 0x0005:
                {
                    var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                    if (subject == null) return;
                    client.LearnSkill(Mundane, subject, "Always refine your skills as much as you sharpen your knife.");

                    break;
                }

            #endregion

            case 0x0017:
                client.SendItemShopDialog(Mundane, "Artifacts I found down here, take a look", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x0018:
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x21, "Where is it?"),
                    new (0x22, "What is it?")
                };
                client.SendOptionsDialog(Mundane, "Ah, yes, the Evermore. What of it?", options.ToArray());
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
                                    NpcShopExtensions.DungeonMediumQuality(), ItemQualityVariance.DetermineVariance(),
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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you");
                            TopMenu(client);
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "Don't try to con a con");
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
            case 0x21:
                client.SendOptionsDialog(Mundane, "You're in it, or at least. The entrance.");
                break;
            case 0x22:
                var options2 = new List<Dialog.OptionsDataItem>
                {
                    new (0x23, "How exactly was it formed?")
                };
                client.SendOptionsDialog(Mundane, "It's an expansive cavern that stretches all the way to Rionnag", options2.ToArray());
                break;
            case 0x23:
                var options3 = new List<Dialog.OptionsDataItem>
                {
                    new (0x24, "Yes, I'm interested")
                };
                client.SendOptionsDialog(Mundane, "Now yer asking too many questions! But I'll give into the curious mind. The Assassins guild built these caverns as a way to travel unnoticed. Interested in learning more?", options3.ToArray());
                break;
            case 0x24:
                if (client.Aisling.QuestManager.AssassinsGuildReputation <= 0)
                {
                    client.Aisling.QuestManager.RionnagReputation++;
                    client.Aisling.QuestManager.AssassinsGuildReputation++;
                }

                client.SendOptionsDialog(Mundane, "Great! Seek out an old seer near the ruins of Dubhaim");
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        client.SendOptionsDialog(Mundane, "I do not have the coin");
    }

    public override void OnGossip(WorldClient client, string message)
    {
        if (!message.Contains("evermore", StringComparison.InvariantCultureIgnoreCase)) return;
        var options = new List<Dialog.OptionsDataItem>
        {
            new(0x0018, "Yes")
        };

        client.SendOptionsDialog(Mundane, "*Glares* Did you say Evermore?", options.ToArray());
    }
}