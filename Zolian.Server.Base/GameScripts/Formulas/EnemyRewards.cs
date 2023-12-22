using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Interfaces;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using static Darkages.Sprites.Item;

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
                var chance = Generator.RandomNumPercentGen();
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

            //var normalChance = Generator.RandomNumPercentGen();
            //if (normalChance > ServerSetup.Instance.GlobalItemTemplateCache[drop].DropRate) continue;

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
            ServerSetup.Instance.GlobalGroundItemCache.TryAdd(item.ItemId, item);

            if (item.Enchantable && item.ItemQuality is Item.Quality.Epic or Item.Quality.Legendary or Item.Quality.Forsaken)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(88, false));
                });
            }

            if (item.Enchantable && item.ItemQuality is Item.Quality.Mythic)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(157, false));
                });
            }
        }
    }

    private static List<Item> BuildItemList(List<Item> itemsList)
    {
        var buildItemsList = new List<Item>();

        foreach (var item in itemsList)
        {
            var chance = Generator.RandomNumPercentGen();
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
    }

    private void GenerateExperience(Aisling player, bool canCrit = false)
    {
        var exp = (int)_monster.Experience;

        if (canCrit)
        {
            var critical = Generator.RandomNumPercentGen();

            if (critical >= .85)
            {
                exp *= 2;
                player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(341, null, player.Serial));
            }
        }

        var difference = player.ExpLevel - _monster.Template.Level;
        exp = difference switch
        {
            // Monster is higher level than player
            <= -30 => (int)(exp * 0.25),
            <= -15 => (int)(exp * 0.5),
            <= -10 => (int)(exp * 0.75),
            // Monster is lower level than player
            >= 30 => 1,
            >= 15 => (int)(exp * 0.33),
            >= 10 => (int)(exp * 0.66),
            _ => exp
        };

        // Enqueue experience event
        if (player.WithinRangeOf(_monster, 13))
            player.Client.EnqueueExperienceEvent(player, exp, true, false);

        if (player.PartyMembers == null) return;
        
        // Enqueue experience event for party members
        foreach (var party in player.PartyMembers.Where(party => party.Serial != player.Serial))
        {
            if (party.Map != _monster.Map) continue;
            if (party.WithinRangeOf(_monster, 13))
                party.Client.EnqueueExperienceEvent(party, exp, true, false);
        }
    }

    private void GenerateAbility(Aisling player, bool canCrit = false)
    {
        var ap = (int)_monster.Ability;

        if (canCrit)
        {
            var critical = Generator.RandomNumPercentGen();

            if (critical >= .85)
            {
                ap *= 2;
                player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(386, null, player.Serial));
            }
        }

        var difference = player.ExpLevel - _monster.Template.Level;
        ap = difference switch
        {
            // Monster is higher level than player
            <= -30 => (int)(ap * 0.25),
            <= -15 => (int)(ap * 0.5),
            <= -10 => (int)(ap * 0.75),
            // Monster is lower level than player
            >= 30 => 1,
            >= 15 => (int)(ap * 0.25),
            >= 10 => (int)(ap * 0.5),
            _ => ap
        };

        // Enqueue experience event
        player.Client.EnqueueAbilityEvent(player, ap, true, false);

        if (player.PartyMembers == null) return;
        
        // Enqueue experience event for party members
        foreach (var party in player.PartyMembers.Where(party => party.Serial != player.Serial))
        {
            if (party.ExpLevel < 250 || party.Stage < ClassStage.Master)
            {
                party.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=sNot able to earn ability points yet");
                continue;
            }

            if (party.Map != player.Map) continue;
            if (party.WithinRangeOf(player, 13))
                party.Client.EnqueueAbilityEvent(party, ap, true, false);
        }
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
        var templateDrops = monster.Template.Drops;
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
        if (templateDrops.Count > 0)
            dropList.AddRange(templateDrops);
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