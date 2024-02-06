using Chaos.Common.Definitions;

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

        switch (Item.Template.Name)
        {
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

            #region Quest Items

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
            case "Captured Golden Floppy":
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Not sure if I should have ate that");
                    return;
                }

            #endregion
            #region Mining

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

            #endregion
            #region Blacksmithing

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

            #endregion
            #region Hair-dye & Style

            case "Lavender Hair-dye":
                client.Aisling.HairColor = 0;
                client.Aisling.OldColor = 0;
                client.UpdateDisplay();
                return;
            case "Black Hair-dye":
                client.Aisling.HairColor = 1;
                client.Aisling.OldColor = 1;
                client.UpdateDisplay();
                return;
            case "Red Hair-dye":
                client.Aisling.HairColor = 2;
                client.Aisling.OldColor = 2;
                client.UpdateDisplay();
                return;
            case "Orange Hair-dye":
                client.Aisling.HairColor = 3;
                client.Aisling.OldColor = 3;
                client.UpdateDisplay();
                return;
            case "Blonde Hair-dye":
                client.Aisling.HairColor = 4;
                client.Aisling.OldColor = 4;
                client.UpdateDisplay();
                return;
            case "Cyan Hair-dye":
                client.Aisling.HairColor = 5;
                client.Aisling.OldColor = 5;
                client.UpdateDisplay();
                return;
            case "Blue Hair-dye":
                client.Aisling.HairColor = 6;
                client.Aisling.OldColor = 6;
                client.UpdateDisplay();
                return;
            case "Mulberry Hair-dye":
                client.Aisling.HairColor = 7;
                client.Aisling.OldColor = 7;
                client.UpdateDisplay();
                return;
            case "Olive Hair-dye":
                client.Aisling.HairColor = 8;
                client.Aisling.OldColor = 8;
                client.UpdateDisplay();
                return;
            case "Green Hair-dye":
                client.Aisling.HairColor = 9;
                client.Aisling.OldColor = 9;
                client.UpdateDisplay();
                return;
            case "Fire Hair-dye":
                client.Aisling.HairColor = 10;
                client.Aisling.OldColor = 10;
                client.UpdateDisplay();
                return;
            case "Brown Hair-dye":
                client.Aisling.HairColor = 11;
                client.Aisling.OldColor = 11;
                client.UpdateDisplay();
                return;
            case "Grey Hair-dye":
                client.Aisling.HairColor = 12;
                client.Aisling.OldColor = 12;
                client.UpdateDisplay();
                return;
            case "Navy Hair-dye":
                client.Aisling.HairColor = 13;
                client.Aisling.OldColor = 13;
                client.UpdateDisplay();
                return;
            case "Tan Hair-dye":
                client.Aisling.HairColor = 14;
                client.Aisling.OldColor = 14;
                client.UpdateDisplay();
                return;
            case "White Hair-dye":
                client.Aisling.HairColor = 15;
                client.Aisling.OldColor = 15;
                client.UpdateDisplay();
                return;
            case "Pink Hair-dye":
                client.Aisling.HairColor = 16;
                client.Aisling.OldColor = 16;
                client.UpdateDisplay();
                return;
            case "Chartreuse Hair-dye":
                client.Aisling.HairColor = 17;
                client.Aisling.OldColor = 17;
                client.UpdateDisplay();
                return;
            case "Golden Hair-dye":
                client.Aisling.HairColor = 18;
                client.Aisling.OldColor = 18;
                client.UpdateDisplay();
                return;
            case "Lemon Hair-dye":
                client.Aisling.HairColor = 19;
                client.Aisling.OldColor = 19;
                client.UpdateDisplay();
                return;
            case "Royal Hair-dye":
                client.Aisling.HairColor = 20;
                client.Aisling.OldColor = 20;
                client.UpdateDisplay();
                return;
            case "Platinum Hair-dye":
                client.Aisling.HairColor = 21;
                client.Aisling.OldColor = 21;
                client.UpdateDisplay();
                return;
            case "Lilac Hair-dye":
                client.Aisling.HairColor = 22;
                client.Aisling.OldColor = 22;
                client.UpdateDisplay();
                return;
            case "Fuchsia Hair-dye":
                client.Aisling.HairColor = 23;
                client.Aisling.OldColor = 23;
                client.UpdateDisplay();
                return;
            case "Magenta Hair-dye":
                client.Aisling.HairColor = 24;
                client.Aisling.OldColor = 24;
                client.UpdateDisplay();
                return;
            case "Peacock Hair-dye":
                client.Aisling.HairColor = 25;
                client.Aisling.OldColor = 25;
                client.UpdateDisplay();
                return;
            case "Neon Pink Hair-dye":
                client.Aisling.HairColor = 26;
                client.Aisling.OldColor = 26;
                client.UpdateDisplay();
                return;
            case "Arctic Hair-dye":
                client.Aisling.HairColor = 27;
                client.Aisling.OldColor = 27;
                client.UpdateDisplay();
                return;
            case "Mauve Hair-dye":
                client.Aisling.HairColor = 28;
                client.Aisling.OldColor = 28;
                client.UpdateDisplay();
                return;
            case "Neon Orange Hair-dye":
                client.Aisling.HairColor = 29;
                client.Aisling.OldColor = 29;
                client.UpdateDisplay();
                return;
            case "Sky Hair-dye":
                client.Aisling.HairColor = 30;
                client.Aisling.OldColor = 30;
                client.UpdateDisplay();
                return;
            case "Neon Green Hair-dye":
                client.Aisling.HairColor = 31;
                client.Aisling.OldColor = 31;
                client.UpdateDisplay();
                return;
            case "Pistachio Hair-dye":
                client.Aisling.HairColor = 32;
                client.Aisling.OldColor = 32;

                client.UpdateDisplay();
                return;
            case "Corn Hair-dye":
                client.Aisling.HairColor = 33;
                client.Aisling.OldColor = 33;
                client.UpdateDisplay();
                return;
            case "Cerulean Hair-dye":
                client.Aisling.HairColor = 34;
                client.Aisling.OldColor = 34;
                client.UpdateDisplay();
                return;
            case "Chocolate Hair-dye":
                client.Aisling.HairColor = 35;
                client.Aisling.OldColor = 35;
                client.UpdateDisplay();
                return;
            case "Ruby Hair-dye":
                client.Aisling.HairColor = 36;
                client.Aisling.OldColor = 36;
                client.UpdateDisplay();
                return;
            case "Hunter Hair-dye":
                client.Aisling.HairColor = 37;
                client.Aisling.OldColor = 37;
                client.UpdateDisplay();
                return;
            case "Crimson Hair-dye":
                client.Aisling.HairColor = 38;
                client.Aisling.OldColor = 38;
                client.UpdateDisplay();
                return;
            case "Ocean Hair-dye":
                client.Aisling.HairColor = 39;
                client.Aisling.OldColor = 39;
                client.UpdateDisplay();
                return;
            case "Ginger Hair-dye":
                client.Aisling.HairColor = 40;
                client.Aisling.OldColor = 40;
                client.UpdateDisplay();
                return;
            case "Mustard Hair-dye":
                client.Aisling.HairColor = 41;
                client.Aisling.OldColor = 41;
                client.UpdateDisplay();
                return;
            case "Apple Hair-dye":
                client.Aisling.HairColor = 42;
                client.Aisling.OldColor = 42;
                client.UpdateDisplay();
                return;
            case "Leaf Hair-dye":
                client.Aisling.HairColor = 43;
                client.Aisling.OldColor = 43;
                client.UpdateDisplay();
                return;
            case "Cobalt Hair-dye":
                client.Aisling.HairColor = 44;
                client.Aisling.OldColor = 44;
                client.UpdateDisplay();
                return;
            case "Strawberry Hair-dye":
                client.Aisling.HairColor = 45;
                client.Aisling.OldColor = 45;
                client.UpdateDisplay();
                return;
            case "Unusual Hair-dye":
                client.Aisling.HairColor = 46;
                client.Aisling.OldColor = 46;
                client.UpdateDisplay();
                return;
            case "Sea Hair-dye":
                client.Aisling.HairColor = 47;
                client.Aisling.OldColor = 47;
                client.UpdateDisplay();
                return;
            case "Harlequin Hair-dye":
                client.Aisling.HairColor = 48;
                client.Aisling.OldColor = 48;
                client.UpdateDisplay();
                return;
            case "Amethyst Hair-dye":
                client.Aisling.HairColor = 49;
                client.Aisling.OldColor = 49;
                client.UpdateDisplay();
                return;
            case "Neon Red Hair-dye":
                client.Aisling.HairColor = 50;
                client.Aisling.OldColor = 50;
                client.UpdateDisplay();
                return;
            case "Neon Yellow Hair-dye":
                client.Aisling.HairColor = 51;
                client.Aisling.OldColor = 51;
                client.UpdateDisplay();
                return;
            case "Rose Hair-dye":
                client.Aisling.HairColor = 52;
                client.Aisling.OldColor = 52;
                client.UpdateDisplay();
                return;
            case "Salmon Hair-dye":
                client.Aisling.HairColor = 53;
                client.Aisling.OldColor = 53;
                client.UpdateDisplay();
                return;
            case "Scarlet Hair-dye":
                client.Aisling.HairColor = 54;
                client.Aisling.OldColor = 54;
                client.UpdateDisplay();
                return;
            case "Honey Hair-dye":
                client.Aisling.HairColor = 55;
                client.Aisling.OldColor = 55;
                client.UpdateDisplay();
                return;
            case "Special Cut #1":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #2":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #3":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #4":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #5":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #6":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #7":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #8":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #9":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #10":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #11":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #12":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #13":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #14":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #15":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #16":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #17":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #18":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #19":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #20":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #21":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #22":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #23":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #24":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #25":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #26":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #27":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #28":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #29":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
            case "Special Cut #30":
                client.Aisling.HairStyle = 0;
                client.UpdateDisplay();
                return;
                #endregion
        }
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