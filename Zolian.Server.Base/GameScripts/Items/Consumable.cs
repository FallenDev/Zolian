﻿using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using ServiceStack;

namespace Darkages.GameScripts.Items;

[Script("Consumable")]
public class Consumable(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        #region Quest Items

        switch (Item.Template.Name)
        {
            case "Stocking Stuffer":
                {
                    var rand = Generator.RandomNumPercentGen();
                    var stockingItem = new Item();
                    var quality = ItemQualityVariance.DetermineQuality();
                    var variance = ItemQualityVariance.DetermineVariance();
                    var wVariance = ItemQualityVariance.DetermineWeaponVariance();

                    switch (rand)
                    {
                        case > 0 and <= 0.10:
                            stockingItem = stockingItem.Create(aisling, "Santa's Costume", quality, variance, wVariance);
                            break;
                        case > 0.10 and <= 0.20:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Helm A", quality, variance, wVariance);
                            break;
                        case > 0.20 and <= 0.30:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Helm B", quality, variance, wVariance);
                            break;
                        case > 0.30 and <= 0.40:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Helm C", quality, variance, wVariance);
                            break;
                        case > 0.40 and <= 0.50:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Costume A", quality, variance, wVariance);
                            break;
                        case > 0.50 and <= 0.60:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Costume B", quality, variance, wVariance);
                            break;
                        case > 0.60 and <= 0.70:
                            stockingItem = stockingItem.Create(aisling, "Frosty's Costume C", quality, variance, wVariance);
                            break;
                        case > 0.70 and <= 0.83:
                            stockingItem = stockingItem.Create(aisling, "Rudolph's Helm", quality, variance, wVariance);
                            break;
                        case > 0.83 and <= 0.95:
                            stockingItem = stockingItem.Create(aisling, "Rudolph's Costume", quality, variance, wVariance);
                            break;
                        case > 0.95 and <= 1:
                            stockingItem = stockingItem.Create(aisling, "Lumber Jack", quality, variance, wVariance);
                            break;
                    }

                    stockingItem.GiveTo(aisling);
                    aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(294, aisling.Position));
                    return;
                }
            case "Necra Scribblings":
                {
                    aisling.Client.SendServerMessage(ServerMessageType.WoodenBoard, "\n\n     Ye alt tot legen Hier das text von alt\r\n     *lich scribblings*\r\n     seta nemka thulu zaaaa \r\n     nema nemka thula zeeee\r\n     seta nemka thali toee");
                    return;
                }
            case "Zolian Guide":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Guide", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Captured Golden Floppy":
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Not sure if I should have ate that");
                    return;
                }
            case "Raw Dark Iron":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("DarkIron", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Raw Copper":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Copper", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Raw Obsidian":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Obsidian", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Raw Cobalt Steel":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("CobaltSteel", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Raw Hybrasyl":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Hybrasyl", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Raw Talos":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Talos", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }
                    return;
                }
            case "Basic Combo Scroll":
                {
                    var skills = new List<Skill>();

                    if (!aisling.ComboManager.Combo1.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo1))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo1);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo2.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo2))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo2);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo3.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo3))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo3);
                            skills.Add(skill);
                        }
                    }

                    foreach (var skill in skills)
                    {
                        if (skill is null) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.First();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;

                        skill.InUse = false;
                    }
                    return;
                }
            case "Advanced Combo Scroll":
                {
                    var skills = new List<Skill>();

                    if (!aisling.ComboManager.Combo1.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo1))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo1);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo2.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo2))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo2);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo3.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo3))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo3);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo4.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo4))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo4);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo5.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo5))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo5);
                            skills.Add(skill);
                        }
                    }

                    foreach (var skill in skills)
                    {
                        if (skill is null) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.First();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;

                        skill.InUse = false;
                    }
                    return;
                }
            case "Enhanced Combo Scroll":
                {
                    var skills = new List<Skill>();

                    if (!aisling.ComboManager.Combo1.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo1))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo1);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo2.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo2))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo2);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo3.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo3))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo3);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo4.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo4))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo4);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo5.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo5))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo5);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo6.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo6))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo6);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo7.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo7))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo7);
                            skills.Add(skill);
                        }
                    }

                    foreach (var skill in skills)
                    {
                        if (skill is null) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.First();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;

                        skill.InUse = false;
                    }
                    return;
                }
            case "Enchanted Combo Scroll":
                {
                    var skills = new List<Skill>();

                    if (!aisling.ComboManager.Combo1.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo1))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo1);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo2.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo2))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo2);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo3.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo3))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo3);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo4.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo4))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo4);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo5.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo5))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo5);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo6.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo6))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo6);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo7.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo7))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo7);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo8.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo8))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo8);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo9.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo9))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo9);
                            skills.Add(skill);
                        }
                    }
                    if (!aisling.ComboManager.Combo10.IsNullOrEmpty())
                    {
                        if (aisling.SkillBook.HasSkill(aisling.ComboManager.Combo10))
                        {
                            var skill = aisling.GetSkill(aisling.ComboManager.Combo10);
                            skills.Add(skill);
                        }
                    }

                    foreach (var skill in skills)
                    {
                        if (skill is null) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.First();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;

                        skill.InUse = false;
                    }
                    return;
                }
        }

        #endregion

        #region Hair Products

        switch (Item.Template.Name)
        {
            case "Lavender Hairdye":
                client.Aisling.HairColor = 0;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Black Hairdye":
                client.Aisling.HairColor = 1;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Red Hairdye":
                client.Aisling.HairColor = 2;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Orange Hairdye":
                client.Aisling.HairColor = 3;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Blonde Hairdye":
                client.Aisling.HairColor = 4;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Cyan Hairdye":
                client.Aisling.HairColor = 5;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Blue Hairdye":
                client.Aisling.HairColor = 6;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Mulberry Hairdye":
                client.Aisling.HairColor = 7;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Olive Hairdye":
                client.Aisling.HairColor = 8;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Green Hairdye":
                client.Aisling.HairColor = 9;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Fire Hairdye":
                client.Aisling.HairColor = 10;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Brown Hairdye":
                client.Aisling.HairColor = 11;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Grey Hairdye":
                client.Aisling.HairColor = 12;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Navy Hairdye":
                client.Aisling.HairColor = 13;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Tan Hairdye":
                client.Aisling.HairColor = 14;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "White Hairdye":
                client.Aisling.HairColor = 15;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Pink Hairdye":
                client.Aisling.HairColor = 16;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Chartreuse Hairdye":
                client.Aisling.HairColor = 17;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Golden Hairdye":
                client.Aisling.HairColor = 18;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Lemon Hairdye":
                client.Aisling.HairColor = 19;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Royal Hairdye":
                client.Aisling.HairColor = 20;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Platinum Hairdye":
                client.Aisling.HairColor = 21;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Lilac Hairdye":
                client.Aisling.HairColor = 22;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Fuchsia Hairdye":
                client.Aisling.HairColor = 23;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Magenta Hairdye":
                client.Aisling.HairColor = 24;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Peacock Hairdye":
                client.Aisling.HairColor = 25;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Neon Pink Hairdye":
                client.Aisling.HairColor = 26;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Arctic Hairdye":
                client.Aisling.HairColor = 27;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Mauve Hairdye":
                client.Aisling.HairColor = 28;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Neon Orange Hairdye":
                client.Aisling.HairColor = 29;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Sky Hairdye":
                client.Aisling.HairColor = 30;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Neon Green Hairdye":
                client.Aisling.HairColor = 31;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Pistachio Hairdye":
                client.Aisling.HairColor = 32;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Corn Hairdye":
                client.Aisling.HairColor = 33;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Cerulean Hairdye":
                client.Aisling.HairColor = 34;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Chocolate Hairdye":
                client.Aisling.HairColor = 35;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Ruby Hairdye":
                client.Aisling.HairColor = 36;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Hunter Hairdye":
                client.Aisling.HairColor = 37;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Crimson Hairdye":
                client.Aisling.HairColor = 38;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Ocean Hairdye":
                client.Aisling.HairColor = 39;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Ginger Hairdye":
                client.Aisling.HairColor = 40;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Mustard Hairdye":
                client.Aisling.HairColor = 41;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Apple Hairdye":
                client.Aisling.HairColor = 42;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Leaf Hairdye":
                client.Aisling.HairColor = 43;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Cobalt Hairdye":
                client.Aisling.HairColor = 44;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Strawberry Hairdye":
                client.Aisling.HairColor = 45;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Unusual Hairdye":
                client.Aisling.HairColor = 46;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Sea Hairdye":
                client.Aisling.HairColor = 47;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Harlequin Hairdye":
                client.Aisling.HairColor = 48;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Amethyst Hairdye":
                client.Aisling.HairColor = 49;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Neon Red Hairdye":
                client.Aisling.HairColor = 50;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Neon Yellow Hairdye":
                client.Aisling.HairColor = 51;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Rose Hairdye":
                client.Aisling.HairColor = 52;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Salmon Hairdye":
                client.Aisling.HairColor = 53;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Scarlet Hairdye":
                client.Aisling.HairColor = 54;
                client.UpdateDisplay();
                Item.Remove();
                return;
            case "Honey Hairdye":
                client.Aisling.HairColor = 55;
                client.UpdateDisplay();
                Item.Remove();
                return;
        }

        #endregion
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }

    public override void OnDropped(Sprite sprite, Position droppedPosition, Area map)
    {
        if (Item == null) return;
        if (!Item.Template.Flags.FlagIsSet(ItemFlags.DropScript)) return;
        if (sprite is not Aisling aisling) return;

        switch (Item.Template.Name)
        {
            case "Cleric's Feather":
                const string script = "Blink";
                ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(script, out var skill);
                if (skill == null) return;
                var scripts = ScriptManager.Load<SkillScript>(script,
                    Skill.Create(1, ServerSetup.Instance.GlobalSkillTemplateCache[script]));
                scripts.TryGetValue(skill.ScriptName, out var skillScript);
                skillScript?.ItemOnDropped(aisling, droppedPosition, map);
                return;
        }
    }
}