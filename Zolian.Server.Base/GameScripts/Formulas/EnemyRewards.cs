using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using static Darkages.Sprites.Entity.Item;

namespace Darkages.GameScripts.Formulas;

[Script("Rewards 1x")]
public class EnemyRewards : RewardScript
{
    private readonly Monster _monster;

    public EnemyRewards(Monster monster, Aisling player)
    {
        _monster = monster;
        _ = player;
    }

    public override void GenerateRewards(Monster monster, Aisling player)
    {
        GenerateExperience(player, true);
        if (monster.Level >= 250 && player.ExpLevel >= 250 && player.Stage >= ClassStage.Master)
            GenerateAbility(player, true);
        GenerateGold();
        GenerateDrops(monster, player);
    }

    public override void GenerateInanimateRewards(Monster monster, Aisling player)
    {
        GenerateGold();
        DetermineDefinedMonsterDrop(monster, player);
    }

    private void DetermineRandomSpecialDrop(Monster monster, Aisling player)
    {
        var dropList = JoinList(monster);
        if (dropList.Count <= 0) return;
        var items = new List<Item>();

        // Build item list based off of rewards a player can receive from the Monster's level
        foreach (var drop in dropList.Where(drop => ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(drop)))
        {
            // Equipment & Enchantable
            if (ServerSetup.Instance.GlobalItemTemplateCache[drop].Flags.FlagIsSet(ItemFlags.Equipable) || ServerSetup.Instance.GlobalItemTemplateCache[drop].Enchantable)
            {
                var chance = Generator.RandomPercentPrecise();
                if (chance > ServerSetup.Instance.GlobalItemTemplateCache[drop].DropRate) continue;

                var equipItem = new Item();

                if (ServerSetup.Instance.GlobalItemTemplateCache[drop].Enchantable)
                {
                    var quality = ItemQualityVariance.DetermineQuality();
                    var variance = ItemQualityVariance.DetermineVariance();
                    var wVariance = ItemQualityVariance.DetermineWeaponVariance();
                    equipItem = equipItem.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop], quality, variance, wVariance);
                    ItemQualityVariance.ItemDurability(equipItem, quality);
                    items.Add(equipItem);
                    continue;
                }

                equipItem = equipItem.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop]);
                ItemQualityVariance.ItemDurability(equipItem, Quality.Common);
                items.Add(equipItem);
                continue;
            }

            var item2 = new Item();
            item2 = item2.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop]);
            items.Add(item2);
        }

        // Build a list of items based on chance
        var buildItemsList = BuildItemList(items);

        var numberOfItems = Generator.RandNumGen3();
        if (numberOfItems == 0) return;

        // Populate a maximum of items based on chance
        var maxTwoOrThreeItemsList = RandomPullMaxItems(buildItemsList, numberOfItems);

        // Display rewards
        foreach (var item in maxTwoOrThreeItemsList)
        {
            item.Release(_monster, _monster.Position);

            if (item.Enchantable && item.ItemQuality is Quality.Epic or Quality.Legendary or Quality.Forsaken)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(88, false));
                });
            }

            if (item.Enchantable && item.ItemQuality is Quality.Mythic)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(157, false));
                });
            }
        }
    }

    private void DetermineDefinedMonsterDrop(Monster monster, Aisling player)
    {
        var templateDrops = monster.Template.Drops;
        if (templateDrops.Count <= 0) return;
        var items = new List<Item>();

        // Build item list based off of rewards a player can receive from the Monster's level
        foreach (var drop in templateDrops.Where(drop => ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(drop)))
        {
            // Equipment & Enchantable
            if (ServerSetup.Instance.GlobalItemTemplateCache[drop].Flags.FlagIsSet(ItemFlags.Equipable) || ServerSetup.Instance.GlobalItemTemplateCache[drop].Enchantable)
            {
                var chance = Generator.RandomPercentPrecise();
                if (chance > ServerSetup.Instance.GlobalItemTemplateCache[drop].DropRate) continue;

                var equipItem = new Item();

                if (ServerSetup.Instance.GlobalItemTemplateCache[drop].Enchantable)
                {
                    var quality = ItemQualityVariance.DetermineQuality();
                    var variance = ItemQualityVariance.DetermineVariance();
                    var wVariance = ItemQualityVariance.DetermineWeaponVariance();
                    equipItem = equipItem.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop], quality, variance, wVariance);
                    ItemQualityVariance.ItemDurability(equipItem, quality);
                    items.Add(equipItem);
                    continue;
                }

                equipItem = equipItem.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop]);
                ItemQualityVariance.ItemDurability(equipItem, Quality.Common);
                items.Add(equipItem);
                continue;
            }

            var chance2 = Generator.RandomPercentPrecise();
            var item2 = new Item();
            item2 = item2.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop]);
            if (chance2 <= item2.Template.DropRate)
                items.Add(item2);
        }

        // Build a list of items based on chance
        var buildItemsList = BuildLowChanceItemList(items);
        var monsterDefinedDrop = RandomPullOneItem(buildItemsList);

        // Display reward
        foreach (var item in monsterDefinedDrop)
        {
            item.Release(_monster, _monster.Position);

            if (item.Enchantable && item.ItemQuality is Quality.Epic or Quality.Legendary or Quality.Forsaken)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(88, false));
                });
            }

            if (item.Enchantable && item.ItemQuality is Quality.Mythic)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(157, false));
                });
            }
        }
    }

    private static List<Item> BuildLowChanceItemList(List<Item> itemsList)
    {
        var buildItemsList = new List<Item>();

        foreach (var item in itemsList)
        {
            var chance = Generator.RandomPercentPrecise();
            switch (chance)
            {
                // If greater than equal 40%, don't add the item
                case >= .40:
                    continue;
                default:
                    buildItemsList.Add(item);
                    continue;
            }
        }

        return buildItemsList;
    }

    private static List<Item> BuildItemList(List<Item> itemsList)
    {
        var buildItemsList = new List<Item>();

        foreach (var item in itemsList)
        {
            var chance = Generator.RandomPercentPrecise();
            switch (chance)
            {
                // If greater than equal 85%, don't add the item
                case >= .85:
                    continue;
                default:
                    buildItemsList.Add(item);
                    continue;
            }
        }

        return buildItemsList;
    }

    private static List<Item> RandomPullOneItem(List<Item> itemsList)
    {
        var randomItem = new List<Item>();

        if (itemsList.Count > 1)
        {
            var item = itemsList.RandomIEnum();
            randomItem.Add(item);
        }
        else
        {
            return itemsList;
        }

        return randomItem;
    }

    private static List<Item> RandomPullMaxItems(List<Item> itemsList, int count)
    {
        var maxTwoOrThreeItemsList = new List<Item>();

        if (itemsList.Count > 1)
        {
            for (var i = 0; i < count; i++)
            {
                var item = itemsList.RandomIEnum();
                maxTwoOrThreeItemsList.Add(item);
            }
        }
        else
        {
            return itemsList;
        }

        return maxTwoOrThreeItemsList;
    }

    private void GenerateDrops(Monster monster, Aisling player)
    {
        DetermineRandomSpecialDrop(monster, player);
        DetermineDefinedMonsterDrop(monster, player);
    }

    private void GenerateExperience(Aisling player, bool canCrit = false)
    {
        var exp = _monster.Experience;

        if (canCrit)
        {
            var critical = Generator.RandomPercentPrecise();

            if (critical >= .85)
            {
                exp *= 2;
                player.SendAnimationNearby(341, null, player.Serial);
            }
        }

        var difference = player.ExpLevel + player.AbpLevel - _monster.Template.Level;
        var soloExp = LevelRestrictionsOnExpAp(exp, difference);

        // Enqueue experience event
        if (player.WithinRangeOf(_monster, 16))
            player.Client.EnqueueExperienceEvent(player, soloExp, true);

        if (player.GroupParty?.PartyMembers == null) return;

        // Enqueue experience event for party members
        foreach (var party in player.GroupParty.PartyMembers.Values.Where(party => party.Serial != player.Serial))
        {
            if (party.Map != _monster.Map) continue;
            if (!party.WithinRangeOf(_monster, 16)) continue;

            var partyDiff = party.ExpLevel + player.AbpLevel - _monster.Template.Level;
            var partyExp = LevelRestrictionsOnExpAp(exp, partyDiff);

            party.Client.EnqueueExperienceEvent(party, partyExp, true);
        }
    }

    private void GenerateAbility(Aisling player, bool canCrit = false)
    {
        var ap = (int)_monster.Ability;

        if (canCrit)
        {
            var critical = Generator.RandomPercentPrecise();

            if (critical >= .85)
            {
                ap *= 2;
                player.SendAnimationNearby(386, null, player.Serial);
            }
        }

        var difference = player.ExpLevel + player.AbpLevel - _monster.Template.Level;
        var soloAp = LevelRestrictionsOnExpAp(ap, difference);

        // Enqueue experience event
        if (player.WithinRangeOf(_monster, 16))
            player.Client.EnqueueAbilityEvent(player, (int)soloAp, true);

        if (player.GroupParty?.PartyMembers == null) return;

        // Enqueue experience event for party members
        foreach (var party in player.GroupParty.PartyMembers.Values.Where(party => party.Serial != player.Serial))
        {
            if (party.ExpLevel < 250 || party.Stage < ClassStage.Master)
            {
                party.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=sNot able to earn ability points yet");
                continue;
            }

            if (party.Map != player.Map) continue;
            if (!party.WithinRangeOf(_monster, 16)) continue;

            var partyDiff = party.ExpLevel + player.AbpLevel - _monster.Template.Level;
            var partyExp = LevelRestrictionsOnExpAp(ap, partyDiff);
            
            party.Client.EnqueueAbilityEvent(party, (int)partyExp, true);
        }
    }

    private static long LevelRestrictionsOnExpAp(long exp, int difference)
    {
        var restrictedExp = difference switch
        {
            // Monster is higher level than player
            <= -80 => 1,
            <= -50 => (long)(exp * 0.25),
            <= -30 => (long)(exp * 0.5),
            <= -15 => (long)(exp * 0.75),
            // Monster is lower level than player
            >= 80 => 1,
            >= 50 => (long)(exp * 0.15),
            >= 30 => (long)(exp * 0.33),
            >= 15 => (long)(exp * 0.66),
            _ => exp
        };

        return restrictedExp;
    }

    private void GenerateGold()
    {
        if (!_monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold)) return;

        var sum = (uint)Random.Shared.Next(_monster.Template.Level * 13, _monster.Template.Level * 200);

        if (_monster.Template.LootType.LootFlagIsSet(LootQualifer.LootGoblinG) ||
            _monster.Template.LootType.LootFlagIsSet(LootQualifer.LootGoblinY) ||
            _monster.Template.LootType.LootFlagIsSet(LootQualifer.LootGoblinP) ||
            _monster.Template.LootType.LootFlagIsSet(LootQualifer.LootGoblinO) ||
            _monster.Template.LootType.LootFlagIsSet(LootQualifer.LootGoblinR))
        {
            sum *= (uint)_monster.Template.LootType;
        }

        if (sum > 0)
            Money.Create(_monster, sum, new Position(_monster.Pos.X, _monster.Pos.Y));
    }

    private static List<string> JoinList(Monster monster)
    {
        var dropList = new List<string>();
        var ring = GenerateDropsBasedOnLevel(monster, RingDrops);
        var belt = GenerateDropsBasedOnLevel(monster, BeltDrops);
        var boot = GenerateDropsBasedOnLevel(monster, BootDrops);
        var earring = GenerateDropsBasedOnLevel(monster, EarringDrops);
        var greaves = GenerateDropsBasedOnLevel(monster, GreaveDrops);
        var hand = GenerateDropsBasedOnLevel(monster, HandDrops);
        var necklace = GenerateDropsBasedOnLevel(monster, NecklaceDrops);
        var offHand = GenerateDropsBasedOnLevel(monster, OffHandDrops);
        var shield = GenerateDropsBasedOnLevel(monster, ShieldDrops);
        var wrist = GenerateDropsBasedOnLevel(monster, WristDrops);
        if (ring != null)
            dropList.AddRange(ring);
        if (belt != null)
            dropList.AddRange(belt);
        if (boot != null)
            dropList.AddRange(boot);
        if (earring != null)
            dropList.AddRange(earring);
        if (greaves != null)
            dropList.AddRange(greaves);
        if (hand != null)
            dropList.AddRange(hand);
        if (necklace != null)
            dropList.AddRange(necklace);
        if (offHand != null)
            dropList.AddRange(offHand);
        if (shield != null)
            dropList.AddRange(shield);
        if (wrist != null)
            dropList.AddRange(wrist);

        return dropList;
    }
}