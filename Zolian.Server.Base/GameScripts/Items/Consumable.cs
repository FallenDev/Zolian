using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
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
        var name = !Item.GiftWrapped.IsNullOrEmpty() ? Item.GiftWrapped : Item.Template.Name;

        switch (name)
        {
            case "Zolian Guide":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Guide", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }

            #region Quest Items

            case "Stocking Stuffer":
                {
                    var rand = Generator.RandomPercentPrecise();
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
                        case > 0.95:
                            stockingItem = stockingItem.Create(aisling, "Lumber Jack", quality, variance, wVariance);
                            break;
                    }

                    stockingItem.GiveTo(aisling);
                    aisling.SendAnimationNearby(294, aisling.Position);
                    return;
                }
            case "Necra Scribblings":
                {
                    client.SendServerMessage(ServerMessageType.WoodenBoard, "\n\n     Ye alt tot legen Hier das text von alt\r\n     *lich scribblings*\r\n     seta nemka thulu zaaaa \r\n     nema nemka thula zeeee\r\n     seta nemka thali toee");
                    return;
                }
            case "Message in a Bottle":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou've freed the parchment from the bottle");
                    var bottle = new Item();
                    bottle = bottle.Create(aisling, "Illegible Treasure Map");
                    bottle.GiveTo(aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, Item);
                    return;
                }
            case "Illegible Treasure Map":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qSomething is written on the back!");
                    client.SendServerMessage(ServerMessageType.ScrollWindow, "If you are reading this, then I've failed in my endeavors. " +
                                                                             "It was my dream to unearth the most hidden treasures this world had to offer, " +
                                                                             "but I'm afraid that I dug too deep this time. I found traces of a hidden treasure " +
                                                                             "rumored to contain untold Legendary Items, but it has proven to be more than I can handle. " +
                                                                             "If you think you are strong enough, I've left some information with my Dear Brother Isaias. " +
                                                                             "He doesn't have the full picture, but it should be enough to get you started if you are smart enough. " +
                                                                             "What fun is a treasure hunt if you have all the answers? May your sails be filled with favorable winds fellow Treasure Hunter!");
                    return;
                }
            case "Enclosed Letter E Sealed":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou've broken the wax seal with an E");
                    var bottle = new Item();
                    bottle = bottle.Create(aisling, "Letter to Edgar");
                    bottle.GiveTo(aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, Item);
                    return;
                }
            case "Enclosed Letter C Sealed":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou've broken the wax seal with a C");
                    var bottle = new Item();
                    bottle = bottle.Create(aisling, "Letter to Corina");
                    bottle.GiveTo(aisling);
                    client.Aisling.Inventory.RemoveFromInventory(client, Item);
                    return;
                }
            case "Letter to Edgar":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qThis letter isn't addressed to you, but you're curious");
                    client.SendServerMessage(ServerMessageType.ScrollWindow, "My dear Brother. I've known we were kindred souls from " +
                                                                             "the moment we met on the training fields. You've " +
                                                                             "helped me grow into the man I am today, even if you " +
                                                                             "didn't know it. Your strength and presence have pushed " +
                                                                             "me to become better in all aspects of my life, and for " +
                                                                             "that I shall be forever grateful. However, it is with " +
                                                                             "a heavy heart that I write this letter, for I know that " +
                                                                             "I shall never return from the battlefield with you after " +
                                                                             "this day. I was given a Divine Vision before we even " +
                                                                             "approached this retched place. I was shown our comrades " +
                                                                             "torn apart and left bloody and dying. The horror of that " +
                                                                             "nightmarish vision was almost enough to make me run from " +
                                                                             "this fight in terror. However, I was given a ray of hope " +
                                                                             "that I might change the events of this day if only I " +
                                                                             "sacrifice myself in your stead. It was no easy decision, " +
                                                                             "but I know that you would have done the same in my position, " +
                                                                             "and that gives me solace. My only regret is that I shall " +
                                                                             "leave my wonderful fiance alone in this world. I beg of " +
                                                                             "you, please search her out and become her strength like " +
                                                                             "you've always been mine. Please accompany her down the " +
                                                                             "rocky road of life and share in each other's grief and " +
                                                                             "happiness. I will forever watch over you both, and I hope " +
                                                                             "that you can forgive my selfishness.                      " +
                                                                             "                 " +
                                                                             "- Your Brother -                                   " +
                                                                             "~ Rouel");
                    return;
                }
            case "Letter to Corina":
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qThis letter isn't addressed to you, but you're curious");
                    client.SendServerMessage(ServerMessageType.ScrollWindow, "My love, I hope that this letter makes it into your " +
                                                                             "hands someday. I am truly sorry that I won't be " +
                                                                             "returning and that I lied to you. I knew I was not " +
                                                                             "destined to return before I even left that day. " +
                                                                             "I was given a divine vision a week before I was " +
                                                                             "destined to leave you. I was shown my brothers in " +
                                                                             "arms being torn apart on the battlefield. They " +
                                                                             "were left torn asunder, helpless and afraid. I was " +
                                                                             "shown that I could save them if I sacrificed myself " +
                                                                             "in their stead. It was not a decision I came too " +
                                                                             "lightly, but I could not leave so many to their fate " +
                                                                             "knowing that I could change the outcome. I hope that " +
                                                                             "you can find it in your heart to forgive me my love, " +
                                                                             "for I know it was a selfish decision to make. If I " +
                                                                             "could ask for a final request, could you please look " +
                                                                             "after Edgar? He is my closest comrade and brother, " +
                                                                             "and I know that he will blame himself for the events " +
                                                                             "yet to unfold. I hope that you both can come together " +
                                                                             "in your grief and share the good times, as well as " +
                                                                             "the bad as you move through life together. I will " +
                                                                             "forever be watching over you from beyond, and you " +
                                                                             "will forever have my heart.        " +
                                                                             "- With Love -                                      " +
                                                                             "~ Rouel");
                    return;
                }
            case "Buried Treasure Chest":
                {
                    if (aisling.HasItem("Moonstone Lockpick"))
                    {
                        var chance = Generator.RandomPercentPrecise();
                        if (chance <= .20)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qClick! Ahh, Gold!!");
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(132, false));
                            client.Aisling.GiveGold((uint)Random.Shared.Next(50000000, 100000000));
                            client.EnqueueExperienceEvent(client.Aisling, Random.Shared.Next(150000000, 250000000), false);
                            var codex = new Item();
                            codex = codex.Create(client.Aisling, "Ancient Smithing Codex");
                            codex.GiveTo(client.Aisling);
                            client.Aisling.Inventory.RemoveFromInventory(client, Item);
                        }

                        var lockpick = aisling.HasItemReturnItem("Moonstone Lockpick");
                        aisling.Inventory.RemoveRange(client, lockpick, 1);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bLockpick snapped!");
                        return;
                    }

                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bIt's Locked!");
                    return;
                }
            case "Breath Sack":
                {
                    aisling.CurrentMp += 5000;
                    if (aisling.CurrentMp > aisling.MaximumMp)
                        aisling.CurrentMp = aisling.MaximumMp;
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You feel refreshed (+5k MP)");
                    aisling.SendAnimationNearby(19, aisling.Position);
                    client.Aisling.Inventory.RemoveRange(client, Item, 1);
                    return;
                }
            case "Captured Golden Floppy":
                {
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Not sure if I should have ate that");
                    return;
                }

            #endregion
            #region Mining

            case "Raw Dark Iron":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("DarkIron", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Raw Copper":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Copper", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Raw Obsidian":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Obsidian", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Raw Cobalt Steel":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("CobaltSteel", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Raw Hybrasyl":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Hybrasyl", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Raw Talos":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Talos", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Chaos Ore":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("ChaosOre", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }

            #endregion
            #region Botony

            case "Gloom Bloom":
                {
                    client.TakeAwayQuantity(client.Aisling, "Gloom Bloom", 1);
                    var enemies = client.Aisling.MonstersNearby().ToList();
                    if (enemies.Count == 0) return;
                    var enemy = enemies.RandomIEnum();

                    if (enemy.HasDebuff("Croich Ard Cradh") ||
                        enemy.HasDebuff("Croich Mor Cradh") ||
                        enemy.HasDebuff("Croich Cradh") ||
                        enemy.HasDebuff("Croich Beag Cradh") ||
                        enemy.HasDebuff("Ard Cradh") ||
                        enemy.HasDebuff("Mor Cradh") ||
                        enemy.HasDebuff("Cradh"))
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Cannot affect, a more potent effect is active");
                        return;
                    }

                    if (enemy.HasDebuff("Beag Cradh"))
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Already affected by Gloom's pollen");
                        return;
                    }

                    if (enemy.SpellReflect)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been reflected!");
                        return;
                    }

                    if (enemy.SpellNegate)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been negated!");
                        return;
                    }

                    aisling.SendAnimationNearby(259, enemy.Position);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(27, false));
                    var debuff = new DebuffBeagcradh();
                    client.EnqueueDebuffAppliedEvent(enemy, debuff);
                    return;
                }
            case "Betrayal Blossom":
                {
                    var chance = Generator.RandomPercentPrecise();
                    aisling.Debuffs.TryGetValue("Skulled", out var skulled);
                    aisling.SendAnimationNearby(49, aisling.Position);
                    client.TakeAwayQuantity(client.Aisling, "Betrayal Blossom", 1);

                    if (chance >= .50)
                    {
                        if (!aisling.Skulled) return;
                        if (skulled == null) return;
                        skulled.Cancelled = true;
                        skulled.OnEnded(aisling, skulled);
                        client.Revive();
                        return;
                    }

                    skulled?.OnEnded(aisling, skulled);
                    return;
                }
            case "Bocan Branch":
                {
                    client.TakeAwayQuantity(client.Aisling, "Bocan Branch", 1);
                    var enemies = client.Aisling.MonstersNearby().ToList();
                    if (enemies.Count == 0) return;
                    var enemy = enemies.RandomIEnum();

                    if (enemy.IsFrozen || enemy.IsSleeping || enemy.Level >= 300)
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Pollen doesn't seem to affect them");
                        return;
                    }

                    if (enemy.SpellReflect)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been reflected!");
                        return;
                    }

                    if (enemy.SpellNegate)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been negated!");
                        return;
                    }

                    aisling.SendAnimationNearby(106, enemy.Position);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(26, false));
                    var debuff = new DebuffDarkChain();
                    client.EnqueueDebuffAppliedEvent(enemy, debuff);
                    return;
                }
            case "Cactus Lilium":
                {
                    client.TakeAwayQuantity(client.Aisling, "Cactus Lilium", 1);
                    var enemies = client.Aisling.MonstersNearby().ToList();
                    if (enemies.Count == 0) return;
                    var enemy = enemies.RandomIEnum();

                    if (enemy.IsBeagParalyzed || enemy.Level >= 220)
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Pollen doesn't seem to affect them");
                        return;
                    }

                    if (enemy.SpellReflect)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been reflected!");
                        return;
                    }

                    if (enemy.SpellNegate)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been negated!");
                        return;
                    }

                    aisling.SendAnimationNearby(95, enemy.Position);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(28, false));
                    var debuff = new DebuffBeagsuain();
                    client.EnqueueDebuffAppliedEvent(enemy, debuff);
                    return;
                }
            case "Prahed Bellis":
                {
                    client.TakeAwayQuantity(client.Aisling, "Prahed Bellis", 1);
                    var enemies = client.Aisling.MonstersNearby().ToList();
                    if (enemies.Count == 0) return;
                    var enemy = enemies.RandomIEnum();

                    if (enemy.IsSleeping || enemy.Level >= 155)
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Pollen doesn't seem to affect them");
                        return;
                    }

                    if (enemy.SpellReflect)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been reflected!");
                        return;
                    }

                    if (enemy.SpellNegate)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been negated!");
                        return;
                    }

                    aisling.SendAnimationNearby(107, enemy.Position);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(32, false));
                    var debuff = new DebuffSleep();
                    client.EnqueueDebuffAppliedEvent(enemy, debuff);
                    return;
                }
            case "Aiten Bloom":
                {
                    client.TakeAwayQuantity(client.Aisling, "Aiten Bloom", 1);
                    var chance = Generator.RandomPercentPrecise();
                    if (aisling.IsAited)
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Pollen doesn't seem to affect you");
                        return;
                    }

                    if (chance >= .50)
                    {
                        aisling.SendAnimationNearby(122, aisling.Position);
                        var buff = new buff_DiaAite();
                        client.EnqueueBuffAppliedEvent(aisling, buff);
                        return;
                    }

                    aisling.SendAnimationNearby(193, aisling.Position);
                    var debuff = new DebuffSunSeal();
                    client.EnqueueDebuffAppliedEvent(aisling, debuff);
                    return;
                }
            case "Reict Weed":
                {
                    client.TakeAwayQuantity(client.Aisling, "Reict Weed", 1);
                    var enemies = client.Aisling.MonstersNearby().ToList();
                    if (enemies.Count == 0) return;
                    var enemy = enemies.RandomIEnum();

                    if (enemy.IsPoisoned || enemy.Level >= 155)
                    {
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Pollen doesn't seem to affect them");
                        return;
                    }

                    if (enemy.SpellReflect)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been reflected!");
                        return;
                    }

                    if (enemy.SpellNegate)
                    {
                        aisling.SendAnimationNearby(184, null, enemy.Serial);
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "The effect from the pollen has been negated!");
                        return;
                    }

                    aisling.SendAnimationNearby(196, enemy.Position);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(137, false));
                    var debuff = new DebuffArdPoison();
                    client.EnqueueDebuffAppliedEvent(enemy, debuff);
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
                        if (skill?.Scripts is null || skill.Scripts.IsEmpty) continue;
                        if (skill.Template.Cooldown == 0)
                            if (!skill.CanUseZeroLineAbility) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.InUse) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.FirstOrDefault();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;
                        script?.OnCleanup();

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
                        if (skill?.Scripts is null || skill.Scripts.IsEmpty) continue;
                        if (skill.Template.Cooldown == 0)
                            if (!skill.CanUseZeroLineAbility) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.InUse) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.FirstOrDefault();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;
                        script?.OnCleanup();

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
                        if (skill?.Scripts is null || skill.Scripts.IsEmpty) continue;
                        if (skill.Template.Cooldown == 0)
                            if (!skill.CanUseZeroLineAbility) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.InUse) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.FirstOrDefault();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;
                        script?.OnCleanup();

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
                        if (skill?.Scripts is null || skill.Scripts.IsEmpty) continue;
                        if (skill.Template.Cooldown == 0)
                            if (!skill.CanUseZeroLineAbility) continue;
                        if (!skill.CanUse()) continue;
                        if (skill.InUse) continue;

                        skill.InUse = true;

                        var script = skill.Scripts.Values.FirstOrDefault();
                        script?.OnUse(aisling);
                        skill.CurrentCooldown = skill.Template.Cooldown;
                        aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                        skill.LastUsedSkill = DateTime.UtcNow;

                        if (skill.Template.SkillType == SkillScope.Assail)
                            aisling.Client.LastAssail = DateTime.UtcNow;
                        script?.OnCleanup();

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
            #region Bags & Chests

            case "Medium Treasure Chest":
                {
                    if (aisling.HasItem("Diamite Lockpick"))
                    {
                        var chance = Generator.RandomPercentPrecise();

                        if (chance <= .80)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qClick! Nice, it opened!");
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(132, false));

                            var rand = Generator.RandomPercentPrecise();
                            var stockingItem = new Item();
                            var quality = ItemQualityVariance.DetermineQuality();
                            var variance = ItemQualityVariance.DetermineVariance();
                            var wVariance = ItemQualityVariance.DetermineWeaponVariance();

                            stockingItem = rand switch
                            {
                                > 0 and <= 0.20 => stockingItem.Create(aisling, "Ruined Arm Guards", quality, variance, wVariance),
                                > 0.20 and <= 0.40 => stockingItem.Create(aisling, "Ruined Shinguards", quality, variance, wVariance),
                                > 0.40 and <= 0.50 => stockingItem.Create(aisling, "Eternal Knot Band", quality, variance, wVariance),
                                > 0.50 and <= 0.60 => stockingItem.Create(aisling, "Enchanted Knot Band", quality, variance, wVariance),
                                > 0.60 and <= 0.80 => stockingItem.Create(aisling, "Gold Pouch", quality, variance, wVariance),
                                > 0.80 => stockingItem.Create(aisling, "Old Cathonic Saber", quality, variance, wVariance),
                                _ => stockingItem
                            };

                            if (aisling.HasItem("Old Cathonic Saber") && stockingItem.Template.Name == "Old Cathonic Saber")
                            {
                                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qYou can only hold one of these. {{=u(Another try given!)");
                                return;
                            }

                            stockingItem.GiveTo(client.Aisling);
                            client.Aisling.Inventory.RemoveFromInventory(client, Item);
                            return;
                        }

                        var lockpick = aisling.HasItemReturnItem("Diamite Lockpick");
                        aisling.Inventory.RemoveRange(client, lockpick, 1);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bLockpick snapped!");
                        return;
                    }

                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bIt's Locked!");
                    return;
                }

            case "Strong Treasure Chest":
                {
                    if (aisling.HasItem("Diamite Lockpick"))
                    {
                        var chance = Generator.RandomPercentPrecise();

                        if (chance <= .80)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qClick! Nice, it opened!");
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(132, false));
                            client.Aisling.GiveGold((uint)Random.Shared.Next(5000000, 10000000));

                            var rand = Generator.RandomPercentPrecise();
                            var stockingItem = new Item();
                            var quality = ItemQualityVariance.DetermineQuality();
                            var variance = ItemQualityVariance.DetermineVariance();
                            var wVariance = ItemQualityVariance.DetermineWeaponVariance();

                            stockingItem = rand switch
                            {
                                > 0 and <= 0.20 => stockingItem.Create(aisling, "Cosmic Sabre", quality, variance,
                                    wVariance),
                                > 0.20 and <= 0.40 => stockingItem.Create(aisling, "Slick Shades", quality, variance,
                                    wVariance),
                                > 0.40 and <= 0.60 => stockingItem.Create(aisling, "Cathonic Shield", quality, variance,
                                    wVariance),
                                > 0.60 and <= 0.80 => stockingItem.Create(aisling, "Kalkuri", quality, variance,
                                    wVariance),
                                > 0.80 => stockingItem.Create(aisling, "Queen's Bow", quality, variance,
                                    wVariance),
                                _ => stockingItem
                            };

                            stockingItem.GiveTo(client.Aisling);
                            client.Aisling.Inventory.RemoveFromInventory(client, Item);
                            return;
                        }

                        var lockpick = aisling.HasItemReturnItem("Diamite Lockpick");
                        aisling.Inventory.RemoveRange(client, lockpick, 1);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bLockpick snapped!");
                        return;
                    }

                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bIt's Locked!");
                    return;
                }
            case "Gold Pouch":
                {
                    var chance = Generator.RandomPercentPrecise();
                    var gold = Random.Shared.Next(15000000, 45000000);
                    var variance = gold * chance;
                    var payout = (uint)(gold + variance);
                    client.Aisling.GiveGold(payout);
                    client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(132, false));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qReceived: {payout} gold!");
                    return;
                }

            case "Santa":
            case "Snowman":
            case "Heart":
                {
                    aisling.SendAnimationNearby(294, aisling.Position);
                    Item.GiftWrapped = string.Empty;
                    Item.DisplayImage = Item.Template.DisplayImage;
                    client.Aisling.Inventory.UpdateSlot(client, Item);
                    return;
                }
            case "Santa Gift Box":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Gift Wrapping Santa", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Snowman Gift Box":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Gift Wrapping Snowman", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }
            case "Heart Gift Box":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (!npc.Value.Scripts.TryGetValue("Gift Wrapping Heart", out var scriptObj)) continue;
                        scriptObj.OnClick(client, npc.Value.Serial);
                        break;
                    }
                    return;
                }

            #endregion
            #region Rift Chests

            case "Rift Boss Chest":
                {
                    if (aisling.HasItem("Moonstone Lockpick"))
                    {
                        var chance = Generator.RandomPercentPrecise();

                        if (chance <= .80)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qClick! Nice, it opened!");
                            aisling.SendAnimationNearby(391, aisling.Position);
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(132, false));
                            client.Aisling.GiveGold((uint)Random.Shared.Next(25_000_000, 42_000_000));

                            var rand = Generator.RandomPercentPrecise();
                            var stockingItem = new Item();
                            var quality = ItemQualityVariance.DetermineHighQuality();
                            var variance = ItemQualityVariance.DetermineVariance();
                            var wVariance = ItemQualityVariance.DetermineWeaponVariance();

                            stockingItem = rand switch
                            {
                                > 0 and <= 0.20 => stockingItem.Create(aisling, "MewMew Claws", quality, variance,
                                    wVariance),
                                > 0.20 and <= 0.40 => stockingItem.Create(aisling, "", quality, variance,
                                    wVariance),
                                > 0.40 and <= 0.60 => stockingItem.Create(aisling, "", quality, variance,
                                    wVariance),
                                > 0.60 and <= 0.80 => stockingItem.Create(aisling, "", quality, variance,
                                    wVariance),
                                > 0.80 => stockingItem.Create(aisling, "", quality, variance,
                                    wVariance),
                                _ => stockingItem
                            };

                            stockingItem.GiveTo(client.Aisling);
                            client.Aisling.Inventory.RemoveFromInventory(client, Item);
                            return;
                        }

                        var lockpick = aisling.HasItemReturnItem("Moonstone Lockpick");
                        aisling.Inventory.RemoveRange(client, lockpick, 1);
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bLockpick snapped!");
                        return;
                    }

                    client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bIt's Locked!");
                    return;
                }

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
                {
                    const string script = "Blink";
                    ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(script, out var skill);
                    if (skill == null) return;
                    var scripts = ScriptManager.Load<SkillScript>(script,
                        Skill.Create(1, ServerSetup.Instance.GlobalSkillTemplateCache[script]));
                    scripts.TryGetValue(skill.ScriptName, out var skillScript);
                    skillScript?.ItemOnDropped(aisling, droppedPosition, map);
                    return;
                }
            case "Chakra Stone":
                {
                    const string script = "Amenotejikara";
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
}