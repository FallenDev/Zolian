using System.Security.Cryptography;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Models;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Formulas;

[Script("Rewards 1x")]
public class EnemyRewards : RewardScript
{
    private readonly Monster _monster;

    public EnemyRewards(Monster monster, Aisling player)
    {
        _monster = monster;
    }

    public override void GenerateRewards(Monster monster, Aisling player)
    {
        GenerateExperience(player, true);
        GenerateGold();
        GenerateDrops(monster, player);
    }

    private void HandleExp(Aisling player, uint exp)
    {
        if (exp <= 0) exp = 1;

        if (player.GroupParty != null)
        {
            var groupSize = player.GroupParty.PartyMembers.Count;
            var adjustment = ServerSetup.Instance.Config.GroupExpBonus;

            if (groupSize > 7)
            {
                adjustment = ServerSetup.Instance.Config.GroupExpBonus = (groupSize - 7) * 0.05;
                if (adjustment < 0.75)
                {
                    adjustment = 0.75;
                }
            }

            var bonus = exp * (1 + player.GroupParty.PartyMembers.Count - 1) * adjustment / 100;
            if (bonus > 0)
                exp += (uint)bonus;
        }

        if (player.ExpLevel <= 98 && _monster.Template.Level <= 98)
        {
            var difference = (int)(player.ExpLevel - _monster.Template.Level);
            exp = difference switch
            {
                // Monster is higher level than player
                <= -30 => (uint)(exp * 0.25),
                <= -15 => (uint)(exp * 0.5),
                <= -10 => (uint)(exp * 0.75),
                // Monster is lower level than player
                >= 30 => 1,
                >= 15 => (uint)(exp * 0.25),
                >= 10 => (uint)(exp * 0.5),
                _ => exp
            };
        }

        player.Client.SendMessage(0x03, $"Received {exp:n0} experience points!");
        player.ExpTotal += exp;
        player.ExpNext -= (int)exp;

        if (player.ExpNext >= int.MaxValue) player.ExpNext = 0;

        var seed = player.ExpLevel * 0.1 + 0.5;
        {
            if (player.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap)
                return;
        }

        while (player.ExpNext <= 0 && player.ExpLevel < 500)
        {
            player.ExpNext = (int)(player.ExpLevel * seed * 5000);

            if (player.ExpLevel == 500)
                break;

            if (player.ExpTotal <= 0)
                player.ExpTotal = uint.MaxValue;

            if (player.ExpTotal >= uint.MaxValue)
                player.ExpTotal = uint.MaxValue;

            if (player.ExpNext <= 0)
                player.ExpNext = 1;

            if (player.ExpNext >= int.MaxValue)
                player.ExpNext = int.MaxValue;

            player.Client.LevelUp(player);
        }

        //while (player.AbpNext <= 0 && player.AbpLevel < 99)
        //{
        // Ab Up
        //}
        // ToDo: Forsaken Dark Ranks
        //if (player.Stage == ClassStage.Forsaken)
        //    AbUp(player);
    }

    private static void AbUp(Player player)
    {
        if (player.AbpLevel >= ServerSetup.Instance.Config.PlayerLevelCap)
            return;

        var wisMod = (double)player._Wis / player.ExpLevel;
        var conMod = (double)player._Con / player.ExpLevel;
        var mpAdd = (int)Math.Round(wisMod * 100 + 25);
        var hpAdd = (int)Math.Round(conMod * 150 + 25);

        player.BaseHp += hpAdd;
        player.BaseMp += mpAdd;
        player.StatPoints += ServerSetup.Instance.Config.StatsPerLevel;
        player.AbpLevel++;

        player.Client.SendMessage(0x02, $"{string.Format(ServerSetup.Instance.Config.AbilityUpMessage, player.AbpLevel)}");
        player.Show(Scope.NearbyAislings,
            new ServerFormat29((uint)player.Serial, (uint)player.Serial, 385, 385, 75));
        var item = new Item();
        item = item.Create(player, "Dark Rank");
        var x = player.Position.X - 2;
        var y = player.Position.Y - 2;
        var pos = new Position(x, y);
        item.Release(player, pos, false);
        player.Client.SendStats(StatusFlags.All);
        Task.Delay(2500).ContinueWith(ct =>
        {
            item.Remove();
        });
    }

    private void DetermineRandomSpecialDrop(Monster monster, Sprite player)
    {
        var dropList = JoinList(monster);
        if (dropList.Count <= 0) return;
        var items = new List<Item>();
        double chance;

        foreach (var drop in dropList.Where(drop => ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(drop)))
        {
            // Equipment & Enchantable
            if (ServerSetup.Instance.GlobalItemTemplateCache[drop].Flags.FlagIsSet(ItemFlags.Equipable) && ServerSetup.Instance.GlobalItemTemplateCache[drop].Enchantable)
            {
                var quality = ItemQualityVariance.DetermineQuality();
                var variance = ItemQualityVariance.DetermineVariance();
                var wVariance = ItemQualityVariance.DetermineWeaponVariance();
                chance = Math.Round(Random.Shared.NextDouble(), 2);

                if (!(chance <= ServerSetup.Instance.GlobalItemTemplateCache[drop].DropRate)) continue;

                var equipItem = new Item();
                equipItem = equipItem.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop], quality, variance, wVariance, true);
                ItemQualityVariance.ItemDurability(equipItem, quality);
                items.Add(equipItem);
                continue;
            }

            // Non-Enchantable
            var equipItem2 = new Item();
            equipItem2 = equipItem2.Create(_monster, ServerSetup.Instance.GlobalItemTemplateCache[drop]);
            items.Add(equipItem2);
        }

        var randEquipItems = new List<Item>();

        switch (items.Count)
        {
            case >= 3:
            {
                for (var i = 3; i > randEquipItems.Count; i--)
                {
                    var item = RandomNumberGenerator.GetInt32(items.Count);
                    chance = Generator.RandNumGen100();
                    var kickOut = Generator.RandNumGen100();
                    if (kickOut >= 90) return;

                    switch (chance)
                    {
                        // If greater than equal 70, restart the list
                        case >= 70:
                            randEquipItems = new List<Item>();
                            continue;
                        // If greater than 30, continue without adding
                        case >= 45 and <= 69:
                            continue;
                        default:
                            randEquipItems.Add(items[item]);
                            continue;
                    }
                }

                break;
            }
            case 2:
            {
                for (var i = 2; i > randEquipItems.Count; i--)
                {
                    var item = RandomNumberGenerator.GetInt32(items.Count);
                    chance = Generator.RandNumGen100();
                    var kickOut = Generator.RandNumGen100();
                    if (kickOut >= 95) return;

                    switch (chance)
                    {
                        // If greater than equal 70, restart the list
                        case >= 70:
                            randEquipItems = new List<Item>();
                            continue;
                        // If greater than 50, continue without adding
                        case >= 50 and <= 69:
                            continue;
                        default:
                            randEquipItems.Add(items[item]);
                            continue;
                    }
                }

                break;
            }
            default:
                randEquipItems = items;
                break;
        }

        foreach (var item in randEquipItems)
        {
            item.Release(_monster, _monster.Position, false);
            if (item.Enchantable && item.ItemQuality is Item.Quality.Epic or Item.Quality.Legendary or Item.Quality.Forsaken)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.Client.SendAnimation(361, player, monster);
                    player.Client.SendSound(88, Scope.NearbyAislings);
                    player.Client.SendSound(157, Scope.NearbyAislings);
                });
            }

            if (item.Enchantable && item.ItemQuality is Item.Quality.Mythic)
            {
                Task.Delay(100).ContinueWith(ct =>
                {
                    player.Client.SendAnimation(359, player, monster);
                    player.Client.SendSound(88, Scope.NearbyAislings);
                    player.Client.SendSound(157, Scope.NearbyAislings);
                });
            }
        }
    }

    private void GenerateDrops(Monster monster, Sprite player)
    {
        DetermineRandomSpecialDrop(monster, player);
    }

    private void GenerateExperience(Aisling player, bool canCrit = false)
    {
        var exp = _monster.Experience;

        if (canCrit)
        {
            var critical = Math.Abs(Generator.GenerateNumber() % 100);

            if (critical >= 85)
            {
                exp *= 2;
                player.SendAnimation(341, player, player);
            }
        }

        HandleExp(player, exp);

        // Distribute the experience to the rest of the team or return and send stats
        if (player.PartyMembers == null) return;

        foreach (var party in player.PartyMembers
                     .Where(party => party.Serial != player.Serial)
                     .Where(party => party.WithinRangeOf(player)))
        {
            HandleExp(party, exp);
        }
    }

    private void GenerateGold()
    {
        if (!_monster.Template.LootType.LootFlagIsSet(LootQualifer.Gold)) return;

        var sum = (uint)Random.Shared.Next((int)(_monster.Template.Level * 13), (int)(_monster.Template.Level * 200));

        if (sum > 0)
            Money.Create(_monster, sum, new Position(_monster.Pos.X, _monster.Pos.Y));
    }

    private List<string> JoinList(Monster monster)
    {
        var dropList = new List<string>();
        var templateDrops = monster.Template.Drops;
        var ring = GenerateRingDropsBasedOnLevel(monster);
        var belt = GenerateBeltDropsBasedOnLevel(monster);
        var boot = GenerateBootDropsBasedOnLevel(monster);
        var earring = GenerateEarringDropsBasedOnLevel(monster);
        var greaves = GenerateGreaveDropsBasedOnLevel(monster);
        var hand = GenerateHandDropsBasedOnLevel(monster);
        var necklace = GenerateNecklaceDropsBasedOnLevel(monster);
        var offHand = GenerateOffHandDropsBasedOnLevel(monster);
        var shield = GenerateShieldDropsBasedOnLevel(monster);
        var wrist = GenerateWristDropsBasedOnLevel(monster);
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