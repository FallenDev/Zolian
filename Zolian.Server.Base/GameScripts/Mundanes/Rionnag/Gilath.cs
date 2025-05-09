﻿using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Rionnag;

[Script("Gilath")]
public class Gilath : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public Gilath(WorldServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
        _spellList = ObtainSpellList();
    }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (_skillList.Count > 0)
        {
            options.Add(new(0x0016, "Show Available Skills"));
        }

        if (_spellList.Count > 0)
        {
            options.Add(new(0x0010, "Show Available Spells"));
        }

        options.Add(new(0x02, "Forget Skill"));
        options.Add(new(0x0011, "Forget Spell"));
        options.Add(new(0x0017, "{=cInventory"));
        options.Add(new(0x0018, "Sell"));


        client.SendOptionsDialog(Mundane,
            client.Aisling.Stage <= ClassStage.Master
                ? "You dare to even look at me?"
                : "Listen well, for not many can survive what I have to teach", options.ToArray());
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

            #region Spells

            case 0x0010:
                {
                    var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                    var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.Path)
                                                                                || i.Prerequisites.ClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.Path)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(client.Aisling.PastClass)
                                                                                || i.Prerequisites.ClassRequired.ClassFlagIsSet(Class.Peasant)
                                                                                || i.Prerequisites.SecondaryClassRequired.ClassFlagIsSet(Class.Peasant)).ToList();

                    newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                    if (newSpells.Count > 0)
                    {
                        client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x0012, newSpells);
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
                    }

                    break;
                }
            case 0x0011:
                {
                    client.SendForgetSpells(Mundane, "The mind is a complex place, sometimes we need to declutter. \nBe warned, This cannot be undone.", 0x0800);
                    break;
                }
            case 0x0012:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x0015, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x0013, "Learn"),
                        new Dialog.OptionsDataItem(0x0010, "No, thank you."));
                    break;
                }
            case 0x0013:
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
                            new Dialog.OptionsDataItem(0x0014, "Yes."),
                            new Dialog.OptionsDataItem(0x0010, "I'll come back later."));
                    }

                    break;
                }
            case 0x0014:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;
                    client.LearnSpell(Mundane, subject, "Always expand your knowledge, Aisling.");

                    break;
                }
            case 0x0015:
                {
                    var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                    if (subject == null) return;

                    client.SendOptionsDialog(Mundane,
                        $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                        subject.Name,
                        new Dialog.OptionsDataItem(0x0013, "Yes"),
                        new Dialog.OptionsDataItem(0x0010, "No"));

                    break;
                }
            case 0x0800:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "I do not sense this spell within you any longer.");
                        client.CloseDialog();
                    }

                    client.Aisling.SpellBook.Remove(client, (byte)idx);
                    client.LoadSpellBook();

                    client.SendForgetSpells(Mundane, "It is gone, Shall we cleanse more?\nRemember, This cannot be undone.", 0x0800);
                    break;
                }

            #endregion

            case 0x0017:
                client.SendItemShopDialog(Mundane, "Various magical items, take a look", NpcShopExtensions.BuyFromStoreInventory(Mundane));
                break;
            case 0x0018:
                client.SendOptionsDialog(Mundane, "I have no need for worldly trinkets");
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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cThank you mortal");
                            TopMenu(client);
                        }
                        else
                        {
                            client.SendOptionsDialog(Mundane, "Leave, less you wish to be a dog");
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

    public override void OnItemDropped(WorldClient client, Item item)
    {
        client.SendOptionsDialog(Mundane, "I have no need for worldly trinkets");
    }

    public override void OnGossip(WorldClient client, string message) { }
}