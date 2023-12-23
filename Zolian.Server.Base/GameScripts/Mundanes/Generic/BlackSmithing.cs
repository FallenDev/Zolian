using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("BlackSmithing")]
public class BlackSmithing(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private string _tempSkillName;

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        switch (client.Aisling.QuestManager.BlackSmithingTier)
        {
            case "Novice": // 25
                //options.Add(new(0x03, "Introduction"));
                options.Add(new(0x00, "Improve Weapons"));
                break;
            case "Apprentice": // 75
                //options.Add(new(0x03, "Introduction"));

                if (client.Aisling.HasItem("Basic Combo Scroll"))
                    options.Add(new(0x10, "Basic Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x00, "Improve Weapons"));
                break;
            case "Journeyman": // 150
                //options.Add(new(0x03, "Introduction"));
                if (client.Aisling.HasItem("Basic Combo Scroll"))
                    options.Add(new(0x00, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x20, "Advanced Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x00, "Advance Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
            case "Expert": // 225
                //options.Add(new(0x03, "Introduction"));

                if (client.Aisling.HasItem("Basic Combo Scroll") || client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x00, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Enhanced Combo Scroll"))
                    options.Add(new(0x30, "Enhanced Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x00, "Enhance Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
            case "Artisan":
                //options.Add(new(0x03, "Introduction"));

                if (client.Aisling.HasItem("Basic Combo Scroll") || client.Aisling.HasItem("Advanced Combo Scroll"))
                    options.Add(new(0x00, "Upgrade Combo Scroll"));
                else if (client.Aisling.HasItem("Enchanted Combo Scroll"))
                    options.Add(new(0x40, "Enchanted Combo Scroll"));
                else
                    options.Add(new(0x01, "Craft Basic Combo Scroll"));

                options.Add(new(0x00, "Enchant Weapons"));
                //options.Add(new(0x00, "{=bDismantle Weapons"));
                break;
        }

        client.SendOptionsDialog(Mundane, "*clank!* *clank!* Oh! Hey there, let's get to work.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
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
                            new(0x101, "Skill 1"),
                            new(0x102, "Skill 2"),
                            new(0x103, "Skill 3")
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
                            new(0x201, "Skill 1"),
                            new(0x202, "Skill 2"),
                            new(0x203, "Skill 3"),
                            new(0x204, "Skill 4"),
                            new(0x205, "Skill 5"),

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
                            new(0x301, "Skill 1"),
                            new(0x302, "Skill 2"),
                            new(0x303, "Skill 3"),
                            new(0x304, "Skill 4"),
                            new(0x305, "Skill 5"),
                            new(0x306, "Skill 6"),
                            new(0x307, "Skill 7")
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
                            new(0x401, "Skill 1"),
                            new(0x402, "Skill 2"),
                            new(0x403, "Skill 3"),
                            new(0x404, "Skill 4"),
                            new(0x405, "Skill 5"),
                            new(0x406, "Skill 6"),
                            new(0x407, "Skill 7"),
                            new(0x408, "Skill 8"),
                            new(0x409, "Skill 9"),
                            new(0x40A, "Skill 10")
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

    /// <summary>
    /// So have a success - on success add it to the player's BlackSmithing Score; After a player reaches a certain score, and there-after
    /// have the npc alert them that they've reached a point in their craft where they can attempt a higher degree. If they succeed, give
    /// them the higher degree. (Legendmark, plus save it to their variables)
    ///
    /// If a player has a higher degree, increase their success by 10%, there will be Five layers of degree
    /// Novice
    /// Apprentice
    /// Journeyman
    /// Expert
    /// Artisan
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="message"></param>
    public override void OnGossip(WorldClient client, string message) { }
}